using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

namespace Controller2k
{
    public enum SlidingState : byte { NONE, STARTING, SLIDING, STOPPING };
    [RequireComponent(typeof(CapsuleCollider))]
    [RequireComponent(typeof(Rigidbody))]
    public class CharacterController2k : MonoBehaviour
    {
        public event Action<CollisionInfo> collision;
        [Header("Player Root")]
        [FormerlySerializedAs("m_PlayerRootTransform")]
        [Tooltip("The root bone in the avatar.")]
        public Transform playerRootTransform;
        [FormerlySerializedAs("m_RootTransformOffset")]
        [Tooltip("The root transform will be positioned at this offset.")]
        public Vector3 rootTransformOffset = Vector3.zero;
        [Header("Collision")]
        [FormerlySerializedAs("m_SlopeLimit")]
        [Tooltip("Limits the collider to only climb slopes that are less steep (in degrees) than the indicated value.")]
        public float slopeLimit = 45.0f;
        [FormerlySerializedAs("m_StepOffset")]
        [Tooltip("The character will step up a stair only if it is closer to the ground than the indicated value. " +
                 "This should not be greater than the Character Controller’s height or it will generate an error. " +
                 "Generally this should be kept as small as possible.")]
        public float stepOffset = 0.3f;
        [FormerlySerializedAs("m_SkinWidth")]
        [Tooltip("Two colliders can penetrate each other as deep as their Skin Width. Larger Skin Widths reduce jitter. " +
                 "Low Skin Width can cause the character to get stuck. A good setting is to make this value 10% of the Radius.")]
        public float skinWidth = 0.08f;
        [FormerlySerializedAs("m_GroundedTestDistance")]
        [Tooltip("Distance to test beneath the character when doing the grounded test. Increase if controller.isGrounded doesn't give the correct results or switches between true/false a lot.")]
        public float groundedTestDistance = 0.002f; 
        [FormerlySerializedAs("m_Center")]
        [Tooltip("This will offset the Capsule Collider in world space, and won’t affect how the Character pivots. " +
                 "Ideally, x and z should be zero to avoid rotating into another collider.")]
        public Vector3 center;
        [FormerlySerializedAs("m_Radius")]
        [Tooltip("Length of the Capsule Collider’s radius. This is essentially the width of the collider.")]
        public float radius = 0.5f;
        [FormerlySerializedAs("m_Height")]
        [Tooltip("The Character’s Capsule Collider height. It should be at least double the radius.")]
        public float height = 2.0f;
        [FormerlySerializedAs("m_CollisionLayerMask")]
        [Tooltip("Layers to test against for collisions.")]
        public LayerMask collisionLayerMask = ~0; 
        [FormerlySerializedAs("m_IsLocalHuman")]
        [Tooltip("Is the character controlled by a local human? If true then more calculations are done for more accurate movement.")]
        public bool isLocalHuman = true;
        [FormerlySerializedAs("m_SlideAlongCeiling")]
        [Tooltip("Can character slide vertically when touching the ceiling? (For example, if ceiling is sloped.)")]
        public bool slideAlongCeiling = true;
        [FormerlySerializedAs("m_SlowAgainstWalls")]
        [Tooltip("Should the character slow down against walls?")]
        public bool slowAgainstWalls = false;
        [FormerlySerializedAs("m_MinSlowAgainstWallsAngle")]
        [Range(0.0f, 90.0f), Tooltip("The minimal angle from which the character will start slowing down on walls.")]
        public float minSlowAgainstWallsAngle = 10.0f;
        [FormerlySerializedAs("m_TriggerQuery")]
        [Tooltip("The desired interaction that cast calls should make against triggers")]
        public QueryTriggerInteraction triggerQuery = QueryTriggerInteraction.Ignore;
        [Header("Sliding")]
        [FormerlySerializedAs("m_SlideDownSlopes")]
        [Tooltip("Should the character slide down slopes when their angle is more than the slope limit?")]
        public bool slideDownSlopes = true;
        [FormerlySerializedAs("m_SlideMaxSpeed")]
        [Tooltip("The maximum speed that the character can slide downwards")]
        public float slideMaxSpeed = 10.0f;
        [FormerlySerializedAs("m_SlideGravityScale")]
        [Tooltip("Gravity multiplier to apply when sliding down slopes.")]
        public float slideGravityMultiplier = 1.0f;
        [FormerlySerializedAs("m_SlideStartTime")]
        [Tooltip("The time (in seconds) after initiating a slide classified as a slide start. Used to disable jumping.")]
        public float slideStartDelay = 0.1f;

        [Tooltip("Slight delay (in seconds) before we stop sliding down slopes. To handle cases where sliding test fails for a few frames.")]
        public float slideStopDelay = 0.1f;        
        const float k_MaxSlopeLimit = 90.0f;
        const float k_MaxSlideAngle = 90.0f;        
        const float k_SlideDownSlopeTestDistance = 1.0f;        
        const float k_PushAwayFromSlopeDistance = 0.001f;        
        const float k_MinCheckSteepSlopeAheadDistance = 0.2f;        
        const float k_MinSkinWidth = 0.0001f;        
        const int k_MaxMoveIterations = 20;        
        const float k_MaxStickToGroundDownDistance = 1.0f;        
        const float k_MinStickToGroundDownDistance = 0.01f;        
        const int k_MaxOverlapColliders = 10;        
        const float k_CollisionOffset = 0.001f;        
        const float k_MinMoveDistance = 0.0001f;        
        const float k_MinStepOffsetHeight = k_MinMoveDistance;        
        const float k_SmallMoveVector = 1e-6f;        
        const float k_MaxAngleToUseRaycastNormal = 5.0f;        
        const float k_RaycastScaleDistance = 2.0f;        
        const float k_SlopeCheckDistanceMultiplier = 5.0f;        
        CapsuleCollider m_CapsuleCollider;        
        Vector3 m_StartPosition;        
        List<MoveVector> m_MoveVectors = new List<MoveVector>();        
        int m_NextMoveVectorIndex;       
        Vector3? m_DownCollisionNormal;        
        StuckInfo m_StuckInfo = new StuckInfo();        
        Dictionary<Collider, CollisionInfo> m_CollisionInfoDictionary = new Dictionary<Collider, CollisionInfo>();        
        readonly Collider[] m_PenetrationInfoColliders = new Collider[k_MaxOverlapColliders];        
        public Vector3 velocity { get; private set; }        
        Vector3 m_DefaultCenter;        
        float m_SlopeMovementOffset;         
        public SlidingState slidingState { get; private set; }     
        float slidingStartedTime;
        float slidingStoppedTime;
        Vector3 transformedCenter { get { return transform.TransformVector(center); } }
        float scaledHeight { get { return height * transform.lossyScale.y; } }
        public bool isGrounded { get; private set; }        
        public CollisionFlags collisionFlags { get; private set; }
        public float defaultHeight { get; private set; }
        public float scaledRadius
        {
            get
            {
                Vector3 scale = transform.lossyScale;
                float maxScale = Mathf.Max(Mathf.Max(scale.x, scale.y), scale.z);
                return radius * maxScale;
            }
        }
        
        public Bounds bounds => m_CapsuleCollider.bounds;
        
        void Awake()
        {
            InitCapsuleColliderAndRigidbody();

            SetRootToOffset();

            m_SlopeMovementOffset =  stepOffset / Mathf.Tan(slopeLimit * Mathf.Deg2Rad);
        }

        void Update()
        {
            UpdateSlideDownSlopes();
        }

        void LateUpdate()
        {
            SetRootToOffset();
        }

#if UNITY_EDITOR
        
        void OnValidate()
        {
            Vector3 position = transform.position;
            ValidateCapsule(false, ref position);
            transform.position = position;
            SetRootToOffset();
        }
        void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.cyan;
            Vector3 footPosition = GetFootWorldPosition(transform.position);
            Gizmos.DrawLine(footPosition + Vector3.left * scaledRadius,
                            footPosition + Vector3.right * scaledRadius);
            Gizmos.DrawLine(footPosition + Vector3.back * scaledRadius,
                            footPosition + Vector3.forward * scaledRadius);            
            Vector3 headPosition = transform.position + transformedCenter + Vector3.up * (scaledHeight / 2.0f + skinWidth);
            Gizmos.DrawLine(headPosition + Vector3.left * scaledRadius,
                            headPosition + Vector3.right * scaledRadius);
            Gizmos.DrawLine(headPosition + Vector3.back * scaledRadius,
                            headPosition + Vector3.forward * scaledRadius);
            Vector3 centerPosition = transform.position + transformedCenter;
            Gizmos.DrawLine(centerPosition + Vector3.left * scaledRadius,
                            centerPosition + Vector3.right * scaledRadius);
            Gizmos.DrawLine(centerPosition + Vector3.back * scaledRadius,
                            centerPosition + Vector3.forward * scaledRadius);
        }
#endif

        public CollisionFlags Move(Vector3 moveVector)
        {
            MoveInternal(moveVector, true);
            return collisionFlags;
        }

        public void SetPosition(Vector3 position, bool updateGrounded)
        {
            transform.position = position;

            if (updateGrounded)
            {
                UpdateGrounded(CollisionFlags.None);
            }
        }
        
        bool ComputePenetration(Vector3 positionOffset,
                                Collider collider, Vector3 colliderPosition, Quaternion colliderRotation,
                                out Vector3 direction, out float distance,
                                bool includeSkinWidth, Vector3 currentPosition)
        {
            if (collider == m_CapsuleCollider)
            {
                direction = Vector3.one;
                distance = 0;
                return false;
            }
            if (includeSkinWidth)
            {
                m_CapsuleCollider.radius = radius + skinWidth;
                m_CapsuleCollider.height = height + (skinWidth * 2.0f);
            }            
            bool result = Physics.ComputePenetration(m_CapsuleCollider,
                                                     currentPosition + positionOffset,
                                                     Quaternion.identity,
                                                     collider, colliderPosition, colliderRotation,
                                                     out direction, out distance);
            if (includeSkinWidth)
            {
                m_CapsuleCollider.radius = radius;
                m_CapsuleCollider.height = height;
            }

            return result;
        }

        public bool CheckCollisionBelow(float distance, out RaycastHit hitInfo, Vector3 currentPosition,
                                        Vector3 offsetPosition,
                                        bool useSphereCast = false,
                                        bool useSecondSphereCast = false,
                                        bool adjustPositionSlightly = false)
        {
            bool didCollide = false;
            float extraDistance = adjustPositionSlightly ? k_CollisionOffset : 0.0f;
            if (!useSphereCast)
            {
#if UNITY_EDITOR
                Vector3 start = GetFootWorldPosition(currentPosition) + offsetPosition + Vector3.up * extraDistance;
                Debug.DrawLine(start, start + Vector3.down * (distance + extraDistance), Color.red);
#endif
                if (Physics.Raycast(GetFootWorldPosition(currentPosition) + offsetPosition + Vector3.up * extraDistance,
                                    Vector3.down,
                                    out hitInfo,
                                    distance + extraDistance,
                                    collisionLayerMask,
                                    triggerQuery))
                {
                    didCollide = true;
                    hitInfo.distance = Mathf.Max(0.0f, hitInfo.distance - extraDistance);
                }
            }
            else
            {
#if UNITY_EDITOR
                Debug.DrawRay(currentPosition, Vector3.down, Color.red); 
                Debug.DrawRay(currentPosition +  new Vector3(scaledRadius, 0.0f), Vector3.down, Color.blue);
                Debug.DrawRay(currentPosition +  new Vector3(-scaledRadius, 0.0f), Vector3.down, Color.blue);
                Debug.DrawRay(currentPosition +  new Vector3(0.0f, 0.0f, scaledRadius), Vector3.down, Color.blue);
                Debug.DrawRay(currentPosition +  new Vector3(0.0f, 0.0f, -scaledRadius), Vector3.down, Color.blue);
#endif
                if (SmallSphereCast(Vector3.down,
                                    skinWidth + distance,
                                    out hitInfo,
                                    offsetPosition,
                                    true, currentPosition))
                {
                    didCollide = true;
                    hitInfo.distance = Mathf.Max(0.0f, hitInfo.distance - skinWidth);
                }

                if (!didCollide && useSecondSphereCast)
                {
                    if (BigSphereCast(Vector3.down,
                                      distance + extraDistance, currentPosition,
                                      out hitInfo,
                                      offsetPosition + Vector3.up * extraDistance,
                                      true))
                    {
                        didCollide = true;
                        hitInfo.distance = Mathf.Max(0.0f, hitInfo.distance - extraDistance);
                    }
                }
            }

            return didCollide;
        }

        public bool TrySetHeightAndCenter(float newHeight, Vector3 newCenter,
                                          bool checkForPenetration,
                                          bool updateGrounded)
        {
            
            if (checkForPenetration &&
                !CanSetHeightAndCenter(newHeight, newCenter))
                return false;
            float oldHeight = height;
            Vector3 oldCenter = center;
            Vector3 oldPosition = transform.position;
            Vector3 virtualPosition = oldPosition;
            bool result = TrySetCenter(newCenter, false, false) &&
                          TrySetHeight(newHeight, false, false, false);
            if (checkForPenetration)
            {
                if (Depenetrate(ref virtualPosition))
                {
                    
                    if (CheckCapsule(virtualPosition))
                    {
                        height = oldHeight;
                        center = oldCenter;
                        transform.position = oldPosition;
                        ValidateCapsule(true, ref virtualPosition);
                        result = false;
                    }
                }
            }

            if (updateGrounded)
            {
                UpdateGrounded(CollisionFlags.None);
            }

            transform.position = virtualPosition;
            return result;
        }

        public bool TryResetHeightAndCenter(bool checkForPenetration, bool updateGrounded)
        {
            return TrySetHeightAndCenter(defaultHeight, m_DefaultCenter, checkForPenetration, updateGrounded);
        }
        public bool TrySetCenter(Vector3 newCenter, bool checkForPenetration, bool updateGrounded)
        {
            
            if (checkForPenetration &&
                !CanSetCenter(newCenter))
                return false;
            Vector3 oldCenter = center;
            Vector3 oldPosition = transform.position;
            Vector3 virtualPosition = oldPosition;
            center = newCenter;
            ValidateCapsule(true, ref virtualPosition);
            bool result = true;
            if (checkForPenetration)
            {
                if (Depenetrate(ref virtualPosition))
                {
                    
                    if (CheckCapsule(virtualPosition))
                    {
                        
                        center = oldCenter;
                        transform.position = oldPosition;
                        ValidateCapsule(true, ref virtualPosition);
                        result = false;
                    }
                }
            }

            if (updateGrounded)
            {
                UpdateGrounded(CollisionFlags.None);
            }

            transform.position = virtualPosition;
            return result;
        }

        public bool TryResetCenter(bool checkForPenetration, bool updateGrounded)
        {
            return TrySetCenter(m_DefaultCenter, checkForPenetration, updateGrounded);
        }

        public float ValidateHeight(float newHeight)
        {
            return Mathf.Clamp(newHeight, radius * 2.0f, float.MaxValue);
        }

        public bool TrySetHeight(float newHeight, bool preserveFootPosition,
                                 bool checkForPenetration,
                                 bool updateGrounded)
        {
            
            if (checkForPenetration &&
                !CanSetHeight(newHeight, preserveFootPosition))
                return false;
            newHeight = ValidateHeight(newHeight);
            Vector3 virtualPosition = transform.position;
            bool changeCenter = preserveFootPosition;
            Vector3 newCenter = changeCenter ? Helpers.CalculateCenterWithSameFootPosition(center, height, newHeight, skinWidth) : center;
            if (Mathf.Approximately(height, newHeight))
            {
                return TrySetCenter(newCenter, checkForPenetration, updateGrounded);
            }

            float oldHeight = height;
            Vector3 oldCenter = center;
            Vector3 oldPosition = transform.position;

            if (changeCenter)
            {
                center = newCenter;
            }

            height = newHeight;
            ValidateCapsule(true, ref virtualPosition);

            bool result = true;
            if (checkForPenetration)
            {
                if (Depenetrate(ref virtualPosition))
                {
                    
                    if (CheckCapsule(virtualPosition))
                    {
                        height = oldHeight;
                        if (changeCenter)
                        {
                            center = oldCenter;
                        }
                        transform.position = oldPosition;
                        ValidateCapsule(true, ref virtualPosition);
                        result = false;
                    }
                }
            }

            if (updateGrounded)
            {
                UpdateGrounded(CollisionFlags.None);
            }

            transform.position = virtualPosition;
            return result;
        }

        readonly Collider[] m_OverlapCapsuleColliders = new Collider[k_MaxOverlapColliders];
        public bool CanSetHeight(float newHeight, bool preserveFootPosition)
        {
            newHeight = ValidateHeight(newHeight);
            bool changeCenter = preserveFootPosition;
            Vector3 newCenter = changeCenter ? Helpers.CalculateCenterWithSameFootPosition(center, height, newHeight, skinWidth) : center;
            if (Mathf.Approximately(height, newHeight))
            {
                return true;
            }
            return CanSetHeightAndCenter(newHeight, newCenter);
        }

        
        public bool CanSetCenter(Vector3 newCenter)
        {
            return CanSetHeightAndCenter(height, newCenter);
        }
        
        public bool CanSetHeightAndCenter(float newHeight, Vector3 newCenter)
        {
            newHeight = ValidateHeight(newHeight);
            Debug.DrawLine(
                Helpers.GetTopSphereWorldPositionSimulated(transform, newCenter, scaledRadius, newHeight),
                Helpers.GetBottomSphereWorldPositionSimulated(transform, newCenter, scaledRadius, newHeight),
                Color.yellow,
                3f
            );

            int hits = Physics.OverlapCapsuleNonAlloc(
                Helpers.GetTopSphereWorldPositionSimulated(transform, newCenter, scaledRadius, newHeight),
                Helpers.GetBottomSphereWorldPositionSimulated(transform, newCenter, scaledRadius, newHeight),
                radius,
                m_OverlapCapsuleColliders,
                collisionLayerMask,
                triggerQuery);

            for (int i = 0; i < hits; ++i)
            {
                if (m_OverlapCapsuleColliders[i] != m_CapsuleCollider)
                {
                    return false;
                }
            }
            return true;
        }

        public bool TryResetHeight(bool preserveFootPosition, bool checkForPenetration, bool updateGrounded)
        {
            return TrySetHeight(defaultHeight, preserveFootPosition, checkForPenetration, updateGrounded);
        }

        public Vector3 GetFootWorldPosition()
        {
            return GetFootWorldPosition(transform.position);
        }

        Vector3 GetFootWorldPosition(Vector3 position)
        {
            return position + transformedCenter + (Vector3.down * (scaledHeight / 2.0f + skinWidth));
        }

        void InitCapsuleColliderAndRigidbody()
        {
            GameObject go = transform.gameObject;
            m_CapsuleCollider = go.GetComponent<CapsuleCollider>();
            m_CapsuleCollider.center = center;
            m_CapsuleCollider.radius = radius;
            m_CapsuleCollider.height = height;
            Rigidbody rigidbody = go.GetComponent<Rigidbody>();
            rigidbody.isKinematic = true;
            rigidbody.useGravity = false;
            defaultHeight = height;
            m_DefaultCenter = center;
        }

        void ValidateCapsule(bool updateCapsuleCollider,
                             ref Vector3 currentPosition,
                             bool checkForPenetration = false,
                             bool updateGrounded = false)
        {
            slopeLimit = Mathf.Clamp(slopeLimit, 0.0f, k_MaxSlopeLimit);
            skinWidth = Mathf.Clamp(skinWidth, k_MinSkinWidth, float.MaxValue);
            float oldHeight = height;
            height = ValidateHeight(height);
            if (m_CapsuleCollider != null)
            {
                if (updateCapsuleCollider)
                {
                    m_CapsuleCollider.center = center;
                    m_CapsuleCollider.radius = radius;
                    m_CapsuleCollider.height = height;
                }
                else if (!Mathf.Approximately(height, oldHeight))
                {
                    m_CapsuleCollider.height = height;
                }
            }

            if (checkForPenetration)
            {
                Depenetrate(ref currentPosition);
            }

            if (updateGrounded)
            {
                UpdateGrounded(CollisionFlags.None);
            }
        }
        
        void MoveInternal(Vector3 moveVector,
                          bool slideWhenMovingDown,
                          bool forceTryStickToGround = false,
                          bool doNotStepOffset = false)
        {
            bool wasGrounded = isGrounded;
            Vector3 moveVectorNoY = new Vector3(moveVector.x, 0, moveVector.z);
            bool tryToStickToGround = wasGrounded && (forceTryStickToGround || (moveVector.y <= 0.0f && moveVectorNoY.sqrMagnitude.NotEqualToZero()));
            m_StartPosition = transform.position;
            collisionFlags = CollisionFlags.None;
            m_CollisionInfoDictionary.Clear();
            m_DownCollisionNormal = null;
            if (moveVector.y > 0.0f && slidingState != SlidingState.NONE)
            {
                Debug.Log("CharacterController2k: a jump stopped sliding: " + slidingState);
                slidingState = SlidingState.NONE;
            }

            MoveLoop(moveVector, tryToStickToGround, slideWhenMovingDown, doNotStepOffset);
            bool doDownCast = tryToStickToGround || moveVector.y <= 0.0f;
            UpdateGrounded(collisionFlags, doDownCast);
            velocity = (transform.position - m_StartPosition) / Time.deltaTime;
            BroadcastCollisionEvent();
        }

        
        void BroadcastCollisionEvent()
        {
            if (collision == null || m_CollisionInfoDictionary == null || m_CollisionInfoDictionary.Count <= 0)
            {
                return;
            }
            foreach (KeyValuePair<Collider, CollisionInfo> kvp in m_CollisionInfoDictionary)
            {
                collision(kvp.Value);
            }
        }

        void UpdateGrounded(CollisionFlags movedCollisionFlags, bool doDownCast = true)
        {
            if ((movedCollisionFlags & CollisionFlags.CollidedBelow) != 0)
            {
                isGrounded = true;
            }
            else if (doDownCast)
            {
                isGrounded = CheckCollisionBelow(groundedTestDistance,
                                                 out RaycastHit hitInfo,
                                                 transform.position,
                                                 Vector3.zero,
                                                 true,
                                                 isLocalHuman,
                                                 isLocalHuman);
            }
            else
            {
                isGrounded = false;
            }
        }

        void MoveLoop(Vector3 moveVector, bool tryToStickToGround, bool slideWhenMovingDown, bool doNotStepOffset)
        {
            m_MoveVectors.Clear();
            m_NextMoveVectorIndex = 0;
            SplitMoveVector(moveVector, slideWhenMovingDown, doNotStepOffset);
            MoveVector remainingMoveVector = m_MoveVectors[m_NextMoveVectorIndex];
            m_NextMoveVectorIndex++;
            bool didTryToStickToGround = false;
            m_StuckInfo.OnMoveLoop();
            Vector3 virtualPosition = transform.position;
            for (int i = 0; i < k_MaxMoveIterations; i++)
            {
                Vector3 refMoveVector = remainingMoveVector.moveVector;
                bool collided = MoveMajorStep(ref refMoveVector, remainingMoveVector.canSlide, didTryToStickToGround, ref virtualPosition);
                remainingMoveVector.moveVector = refMoveVector;
                if (m_StuckInfo.UpdateStuck(virtualPosition, remainingMoveVector.moveVector, moveVector))
                {
                    remainingMoveVector = new MoveVector(Vector3.zero);
                }
                else if (!isLocalHuman && collided)
                {
                    remainingMoveVector.canSlide = false;
                }                
                if (!collided || remainingMoveVector.moveVector.sqrMagnitude.IsEqualToZero())
                {
                    
                    if (m_NextMoveVectorIndex < m_MoveVectors.Count)
                    {
                        remainingMoveVector = m_MoveVectors[m_NextMoveVectorIndex];
                        m_NextMoveVectorIndex++;
                    }
                    else
                    {
                        if (!tryToStickToGround || didTryToStickToGround)
                        {
                            break;
                        } 
                        didTryToStickToGround = true;
                        if (!CanStickToGround(moveVector, out remainingMoveVector))
                        {
                            break;
                        }
                    }
                }

#if UNITY_EDITOR
                if (i == k_MaxMoveIterations - 1)
                {
                    Debug.Log(name + " reached MaxMoveInterations(" + k_MaxMoveIterations + "): remainingVector=" + remainingMoveVector + " moveVector=" + moveVector + " hitCount=" + m_StuckInfo.hitCount);
                }
#endif
            }

            transform.position = virtualPosition;
        }
        
        bool MoveMajorStep(ref Vector3 moveVector, bool canSlide, bool tryGrounding, ref Vector3 currentPosition)
        {
            Vector3 direction = moveVector.normalized;
            float distance = moveVector.magnitude;
            RaycastHit bigRadiusHitInfo;
            RaycastHit smallRadiusHitInfo;
            bool smallRadiusHit;
            bool bigRadiusHit;

            if (!CapsuleCast(direction, distance, currentPosition,
                             out smallRadiusHit, out bigRadiusHit,
                             out smallRadiusHitInfo, out bigRadiusHitInfo,
                             Vector3.zero))
            {
                
                MovePosition(moveVector, null, null, ref currentPosition);
                float penetrationDistance;
                Vector3 penetrationDirection;
                if (GetPenetrationInfo(out penetrationDistance, out penetrationDirection, currentPosition))
                {   
                    MovePosition(penetrationDirection * penetrationDistance, null, null, ref currentPosition);
                }                
                moveVector = Vector3.zero;
                return false;
            }

            
            if (!bigRadiusHit)
            {                
                MoveAwayFromObstacle(ref moveVector, ref smallRadiusHitInfo,
                                     direction, distance,
                                     canSlide,
                                     tryGrounding,
                                     true, ref currentPosition);
                return true;
            }

            
            if (smallRadiusHit && smallRadiusHitInfo.distance < bigRadiusHitInfo.distance)
            {
                MoveAwayFromObstacle(ref moveVector, ref smallRadiusHitInfo,
                                     direction, distance,
                                     canSlide,
                                     tryGrounding,
                                     true, ref currentPosition);
                return true;
            }

            MoveAwayFromObstacle(ref moveVector, ref bigRadiusHitInfo,
                                 direction, distance,
                                 canSlide,
                                 tryGrounding,
                                 false, ref currentPosition);

            return true;
        }

        bool CanStepOffset(Vector3 moveVector)
        {
            float moveVectorMagnitude = moveVector.magnitude;
            Vector3 position = transform.position;
            RaycastHit hitInfo;
            if (!SmallSphereCast(moveVector, moveVectorMagnitude, out hitInfo, Vector3.zero, true, position) &&
                !BigSphereCast(moveVector, moveVectorMagnitude, position, out hitInfo, Vector3.zero, true))
            {
                return false;
            }

            float upDistance = Mathf.Max(stepOffset, k_MinStepOffsetHeight);
            Vector3 horizontal = moveVector * scaledRadius;
            float horizontalSize = horizontal.magnitude;
            horizontal.Normalize();
            Vector3 up = Vector3.up * upDistance;
            if (SmallCapsuleCast(horizontal, skinWidth + horizontalSize, out hitInfo, up, position) ||
                BigCapsuleCast(horizontal, horizontalSize, out hitInfo, up, position))
            {
                return false;
            }

            return !CheckSteepSlopeAhead(moveVector);
        }

        bool CheckSteepSlopeAhead(Vector3 moveVector, bool alsoCheckForStepOffset = true)
        {
            Vector3 direction = moveVector.normalized;
            float distance = moveVector.magnitude;

            if (CheckSteepSlopAhead(direction, distance, Vector3.zero))
            {
                return true;
            }

            if (!alsoCheckForStepOffset || !isLocalHuman)
            {
                return false;
            }

            return CheckSteepSlopAhead(direction,
                                       Mathf.Max(distance, k_MinCheckSteepSlopeAheadDistance),
                                       Vector3.up * stepOffset);
        }

        
        bool CheckSteepSlopAhead(Vector3 direction, float distance, Vector3 offsetPosition)
        {
            RaycastHit bigRadiusHitInfo;
            RaycastHit smallRadiusHitInfo;
            bool smallRadiusHit;
            bool bigRadiusHit;

            if (!CapsuleCast(direction, distance, transform.position,
                             out smallRadiusHit, out bigRadiusHit,
                             out smallRadiusHitInfo, out bigRadiusHitInfo,
                             offsetPosition))
            {
                
                return false;
            }

            RaycastHit hitInfoCapsule = (!bigRadiusHit || (smallRadiusHit && smallRadiusHitInfo.distance < bigRadiusHitInfo.distance))
                                        ? smallRadiusHitInfo
                                        : bigRadiusHitInfo;

            RaycastHit hitInfoRay;
            Vector3 rayOrigin = transform.position + transformedCenter + offsetPosition;

            float offset = Mathf.Clamp(m_SlopeMovementOffset, 0.0f, distance * k_SlopeCheckDistanceMultiplier);
            Vector3 rayDirection = (hitInfoCapsule.point + direction * offset) - rayOrigin;
            if (Physics.Raycast(rayOrigin,
                                rayDirection,
                                out hitInfoRay,
                                rayDirection.magnitude * k_RaycastScaleDistance,
                                collisionLayerMask,
                                triggerQuery) &&
                hitInfoRay.collider == hitInfoCapsule.collider)
            {
                hitInfoCapsule = hitInfoRay;
            }
            else
            {
                return false;
            }

            float slopeAngle = Vector3.Angle(Vector3.up, hitInfoCapsule.normal);
            bool slopeIsSteep = slopeAngle > slopeLimit &&
                                slopeAngle < k_MaxSlopeLimit &&
                                Vector3.Dot(direction, hitInfoCapsule.normal) < 0.0f;

            return slopeIsSteep;
        }
        
        void SplitMoveVector(Vector3 moveVector, bool slideWhenMovingDown, bool doNotStepOffset)
        {
            Vector3 horizontal = new Vector3(moveVector.x, 0.0f, moveVector.z);
            Vector3 vertical = new Vector3(0.0f, moveVector.y, 0.0f);
            bool horizontalIsAlmostZero = Helpers.IsMoveVectorAlmostZero(horizontal, k_SmallMoveVector);
            float tempStepOffset = stepOffset;
            bool doStepOffset = isGrounded &&
                                !doNotStepOffset &&
                                !Mathf.Approximately(tempStepOffset, 0.0f) &&
                                !horizontalIsAlmostZero;
            if (vertical.y > 0.0f)
            {     
                if (horizontal.x.NotEqualToZero() || horizontal.z.NotEqualToZero())
                {
                    AddMoveVector(vertical, slideAlongCeiling);
                    AddMoveVector(horizontal);
                }
                else
                {
                    AddMoveVector(vertical, slideAlongCeiling);
                }
            }
            else if (vertical.y < 0.0f)
            {
                
                if (horizontal.x.NotEqualToZero() || horizontal.z.NotEqualToZero())
                {
                    if (doStepOffset && CanStepOffset(horizontal))
                    {
                        AddMoveVector(Vector3.up * tempStepOffset, false);
                        AddMoveVector(horizontal);
                        if (slideWhenMovingDown)
                        {
                            AddMoveVector(vertical);
                            AddMoveVector(Vector3.down * tempStepOffset);
                        }
                        else
                        {
                            AddMoveVector(vertical + Vector3.down * tempStepOffset);
                        }
                    }
                    else
                    {
                        
                        AddMoveVector(horizontal);
                        AddMoveVector(vertical, slideWhenMovingDown);
                    }
                }
                else
                {
                    
                    AddMoveVector(vertical, slideWhenMovingDown);
                }
            }
            else
            {
                
                if (doStepOffset && CanStepOffset(horizontal))
                {
                    
                    AddMoveVector(Vector3.up * tempStepOffset, false);
                    AddMoveVector(horizontal);
                    AddMoveVector(Vector3.down * tempStepOffset);
                }
                else
                {
                    
                    AddMoveVector(horizontal);
                }
            }
        }
 
        void AddMoveVector(Vector3 moveVector, bool canSlide = true)
        {
            m_MoveVectors.Add(new MoveVector(moveVector, canSlide));
        } 
        
        bool CanStickToGround(Vector3 moveVector, out MoveVector getDownVector)
        {
            Vector3 moveVectorNoY = new Vector3(moveVector.x, 0.0f, moveVector.z);
            float downDistance = Mathf.Max(moveVectorNoY.magnitude, k_MinStickToGroundDownDistance);
            if (moveVector.y < 0.0f)
            {
                downDistance = Mathf.Max(downDistance, Mathf.Abs(moveVector.y));
            }
            if (downDistance <= k_MaxStickToGroundDownDistance)
            {
                getDownVector = new MoveVector(Vector3.down * downDistance, false);
                return true;
            }
            getDownVector = new MoveVector(Vector3.zero);
            return false;
        }

        bool CapsuleCast(Vector3 direction, float distance, Vector3 currentPosition,
                                 out bool smallRadiusHit, out bool bigRadiusHit,
                                 out RaycastHit smallRadiusHitInfo, out RaycastHit bigRadiusHitInfo,
                                 Vector3 offsetPosition)
        {
            
            smallRadiusHit = SmallCapsuleCast(direction, distance, out smallRadiusHitInfo, offsetPosition, currentPosition);
            bigRadiusHit = BigCapsuleCast(direction, distance, out bigRadiusHitInfo, offsetPosition, currentPosition);
            return smallRadiusHit || bigRadiusHit;
        }

        bool SmallCapsuleCast(Vector3 direction, float distance,
                              out RaycastHit smallRadiusHitInfo,
                              Vector3 offsetPosition, Vector3 currentPosition)
        {
            
            
            float extraDistance = scaledRadius;
            if (Physics.CapsuleCast(Helpers.GetTopSphereWorldPosition(currentPosition, transformedCenter, scaledRadius, scaledHeight) + offsetPosition,
                                    Helpers.GetBottomSphereWorldPosition(currentPosition, transformedCenter, scaledRadius, scaledHeight) + offsetPosition,
                                    scaledRadius,
                                    direction,
                                    out smallRadiusHitInfo,
                                    distance + extraDistance,
                                    collisionLayerMask,
                                    triggerQuery))
            {
                return smallRadiusHitInfo.distance <= distance;
            }

            return false;
        }
        
        bool BigCapsuleCast(Vector3 direction, float distance,
                            out RaycastHit bigRadiusHitInfo,
                            Vector3 offsetPosition, Vector3 currentPosition)
        {
            
            float extraDistance = scaledRadius + skinWidth;

            if (Physics.CapsuleCast(Helpers.GetTopSphereWorldPosition(currentPosition, transformedCenter, scaledRadius, scaledHeight) + offsetPosition,
                                    Helpers.GetBottomSphereWorldPosition(currentPosition, transformedCenter, scaledRadius, scaledHeight) + offsetPosition,
                                    scaledRadius + skinWidth,
                                    direction,
                                    out bigRadiusHitInfo,
                                    distance + extraDistance,
                                    collisionLayerMask,
                                    triggerQuery))
            {
                return bigRadiusHitInfo.distance <= distance;
            }

            return false;
        }
        bool SmallSphereCast(Vector3 direction, float distance,
                             out RaycastHit smallRadiusHitInfo,
                             Vector3 offsetPosition,
                             bool useBottomSphere, Vector3 currentPosition)
        {
            float extraDistance = scaledRadius;
            Vector3 spherePosition = useBottomSphere ? Helpers.GetBottomSphereWorldPosition(currentPosition, transformedCenter, scaledRadius, scaledHeight) + offsetPosition
                                                     : Helpers.GetTopSphereWorldPosition(currentPosition, transformedCenter, scaledRadius, scaledHeight) + offsetPosition;
            if (Physics.SphereCast(spherePosition,
                                   scaledRadius,
                                   direction,
                                   out smallRadiusHitInfo,
                                   distance + extraDistance,
                                   collisionLayerMask,
                                   triggerQuery))
            {
                return smallRadiusHitInfo.distance <= distance;
            }

            return false;
        }
        
        bool BigSphereCast(Vector3 direction, float distance, Vector3 currentPosition,
                                   out RaycastHit bigRadiusHitInfo,
                                   Vector3 offsetPosition,
                                   bool useBottomSphere)
        {
            float extraDistance = scaledRadius + skinWidth;
            Vector3 spherePosition = useBottomSphere ? Helpers.GetBottomSphereWorldPosition(currentPosition, transformedCenter, scaledRadius, scaledHeight) + offsetPosition
                                                     : Helpers.GetTopSphereWorldPosition(currentPosition, transformedCenter, scaledRadius, scaledHeight) + offsetPosition;
            if (Physics.SphereCast(spherePosition,
                                   scaledRadius + skinWidth,
                                   direction,
                                   out bigRadiusHitInfo,
                                   distance + extraDistance,
                                   collisionLayerMask,
                                   triggerQuery))
            {
                return bigRadiusHitInfo.distance <= distance;
            }
            return false;
        }
        
        void MoveAwayFromObstacle(ref Vector3 moveVector, ref RaycastHit hitInfoCapsule,
                                  Vector3 direction, float distance,
                                  bool canSlide,
                                  bool tryGrounding,
                                  bool hitSmallCapsule, ref Vector3 currentPosition)
        {
            float collisionOffset = hitSmallCapsule ? skinWidth : k_CollisionOffset;
            float hitDistance = Mathf.Max(hitInfoCapsule.distance - collisionOffset, 0.0f);
            float remainingDistance = Mathf.Max(distance - hitInfoCapsule.distance, 0.0f);
            MovePosition(direction * hitDistance, direction, hitInfoCapsule, ref currentPosition);
            Vector3 hitNormal;
            RaycastHit hitInfoRay;
            Vector3 rayOrigin = currentPosition + transformedCenter;
            Vector3 rayDirection = hitInfoCapsule.point - rayOrigin;           
            if (Physics.Raycast(rayOrigin,
                                rayDirection,
                                out hitInfoRay,
                                rayDirection.magnitude * k_RaycastScaleDistance,
                                collisionLayerMask,
                                triggerQuery) &&
                hitInfoRay.collider == hitInfoCapsule.collider &&
                Vector3.Angle(hitInfoCapsule.normal, hitInfoRay.normal) <= k_MaxAngleToUseRaycastNormal)
            {
                hitNormal = hitInfoRay.normal;
            }
            else
            {
                hitNormal = hitInfoCapsule.normal;
            }
            float penetrationDistance;
            Vector3 penetrationDirection;
            if (GetPenetrationInfo(out penetrationDistance, out penetrationDirection, currentPosition, true, null, hitInfoCapsule))
            {
                MovePosition(penetrationDirection * penetrationDistance, null, null, ref currentPosition);
            }

            bool slopeIsSteep = false;
            if (tryGrounding || m_StuckInfo.isStuck)
            {
                canSlide = false;
            }
            else if (moveVector.x.NotEqualToZero() || moveVector.z.NotEqualToZero())
            {
                float slopeAngle = Vector3.Angle(Vector3.up, hitNormal);
                slopeIsSteep = slopeAngle > slopeLimit && slopeAngle < k_MaxSlopeLimit && Vector3.Dot(direction, hitNormal) < 0.0f;
            }
            
            if (canSlide && remainingDistance > 0.0f)
            {
                Vector3 slideNormal = hitNormal;
                if (slopeIsSteep && slideNormal.y > 0.0f)
                {
                    slideNormal.y = 0.0f;
                    slideNormal.Normalize();
                }

                Vector3 project = Vector3.Cross(direction, slideNormal);
                project = Vector3.Cross(slideNormal, project);

                if (slopeIsSteep && project.y > 0.0f)
                {
                    project.y = 0.0f;
                }
                project.Normalize();                
                bool isWallSlowingDown = slowAgainstWalls && minSlowAgainstWallsAngle < 90.0f;

                if (isWallSlowingDown)
                {
                    float invRescaleFactor = 1 / Mathf.Cos(minSlowAgainstWallsAngle * Mathf.Deg2Rad);
                    float cosine = Vector3.Dot(project, direction);
                    float slowDownFactor = Mathf.Clamp01(cosine * invRescaleFactor);
                    moveVector = project * (remainingDistance * slowDownFactor);
                }
                else
                {
                    moveVector = project * remainingDistance;
                }
            }
            else
            {
                moveVector = Vector3.zero;
            }

            if (direction.y < 0.0f && Mathf.Approximately(direction.x, 0.0f) && Mathf.Approximately(direction.z, 0.0f))
            {
                m_DownCollisionNormal = hitNormal;
            }
        }
        
        bool Depenetrate(ref Vector3 currentPosition)
        {
            float distance;
            Vector3 direction;
            if (GetPenetrationInfo(out distance, out direction, currentPosition))
            {
                MovePosition(direction * distance, null, null, ref currentPosition);
                return true;
            }
            return false;
        }
        
        bool GetPenetrationInfo(out float getDistance, out Vector3 getDirection,
                                Vector3 currentPosition,
                                bool includeSkinWidth = true,
                                Vector3? offsetPosition = null,
                                RaycastHit? hitInfo = null)
        {
            getDistance = 0.0f;
            getDirection = Vector3.zero;
            Vector3 offset = offsetPosition != null ? offsetPosition.Value : Vector3.zero;
            float tempSkinWidth = includeSkinWidth ? skinWidth : 0.0f;
            int overlapCount = Physics.OverlapCapsuleNonAlloc(Helpers.GetTopSphereWorldPosition(currentPosition, transformedCenter, scaledRadius, scaledHeight) + offset,
                                                              Helpers.GetBottomSphereWorldPosition(currentPosition, transformedCenter, scaledRadius, scaledHeight) + offset,
                                                              scaledRadius + tempSkinWidth,
                                                              m_PenetrationInfoColliders,
                                                              collisionLayerMask,
                                                              triggerQuery);
            if (overlapCount <= 0 || m_PenetrationInfoColliders.Length <= 0)
            {
                return false;
            }

            bool result = false;
            Vector3 localPos = Vector3.zero;
            for (int i = 0; i < overlapCount; i++)
            {
                Collider col = m_PenetrationInfoColliders[i];
                if (col == null)
                {
                    break;
                }
                Vector3 direction;
                float distance;
                Transform colliderTransform = col.transform;
                if (ComputePenetration(offset,
                                       col, colliderTransform.position, colliderTransform.rotation,
                                       out direction, out distance, includeSkinWidth, currentPosition))
                {
                    localPos += direction * (distance + k_CollisionOffset);
                    result = true;
                }
                else if (hitInfo != null && hitInfo.Value.collider == col)
                {
                    localPos += hitInfo.Value.normal * k_CollisionOffset;
                    result = true;
                }
            }
            if (result)
            {
                getDistance = localPos.magnitude;
                getDirection = localPos.normalized;
            }
            return result;
        }
        
        bool CheckCapsule(Vector3 currentPosition, bool includeSkinWidth = true,
                          Vector3? offsetPosition = null)
        {
            Vector3 offset = offsetPosition != null ? offsetPosition.Value : Vector3.zero;
            float tempSkinWidth = includeSkinWidth ? skinWidth : 0;
            return Physics.CheckCapsule(Helpers.GetTopSphereWorldPosition(currentPosition, transformedCenter, scaledRadius, scaledHeight) + offset,
                                        Helpers.GetBottomSphereWorldPosition(currentPosition, transformedCenter, scaledRadius, scaledHeight) + offset,
                                        scaledRadius + tempSkinWidth,
                                        collisionLayerMask,
                                        triggerQuery);
        }
        
        void MovePosition(Vector3 moveVector, Vector3? collideDirection, RaycastHit? hitInfo, ref Vector3 currentPosition)
        {
            if (moveVector.sqrMagnitude.NotEqualToZero())
            {
                currentPosition += moveVector;
            }
            if (collideDirection != null && hitInfo != null)
            {
                UpdateCollisionInfo(collideDirection.Value, hitInfo.Value, currentPosition);
            }
        }
        
        void UpdateCollisionInfo(Vector3 direction, RaycastHit? hitInfo, Vector3 currentPosition)
        {
            if (direction.x.NotEqualToZero() || direction.z.NotEqualToZero())
            {
                collisionFlags |= CollisionFlags.Sides;
            }
            if (direction.y > 0.0f)
            {
                collisionFlags |= CollisionFlags.CollidedAbove;
            }
            else if (direction.y < 0.0f)
            {
                collisionFlags |= CollisionFlags.CollidedBelow;
            }
            m_StuckInfo.hitCount++;

            if (hitInfo != null)
            {
                Collider collider = hitInfo.Value.collider;
                if (!m_CollisionInfoDictionary.ContainsKey(collider))
                {
                    Vector3 moved = currentPosition - m_StartPosition;
                    CollisionInfo newCollisionInfo = new CollisionInfo(this, hitInfo.Value, direction, moved.magnitude);
                    m_CollisionInfoDictionary.Add(collider, newCollisionInfo);
                }
            }
        }
        
        bool CastForSlopeNormal(out Vector3 slopeNormal)
        {
            RaycastHit hitInfoSphere;
            if (!SmallSphereCast(Vector3.down,
                                 skinWidth + k_SlideDownSlopeTestDistance,
                                 out hitInfoSphere,
                                 Vector3.zero,
                                 true, transform.position))
            {
                slopeNormal = Vector3.zero;
                return false;
            }
            RaycastHit hitInfoRay;
            Vector3 rayOrigin = Helpers.GetBottomSphereWorldPosition(transform.position, transformedCenter, scaledRadius, scaledHeight);
            Vector3 rayDirection = hitInfoSphere.point - rayOrigin;
            if (Physics.Raycast(rayOrigin,
                                rayDirection,
                                out hitInfoRay,
                                rayDirection.magnitude * k_RaycastScaleDistance,
                                collisionLayerMask,
                                triggerQuery) &&
                hitInfoRay.collider == hitInfoSphere.collider)
            {
                slopeNormal = hitInfoRay.normal;
            }
            else
            {
                slopeNormal = hitInfoSphere.normal;
            }
            return true;
        }        
        
        bool ReuseOrCastForSlopeNormal(out Vector3 slopeNormal)
        {
            bool onSlopeSurface = slidingState == SlidingState.STARTING ||
                                  slidingState == SlidingState.SLIDING;
            if (onSlopeSurface && m_DownCollisionNormal != null)
            {
                slopeNormal = m_DownCollisionNormal.Value;
                return true;
            }
            else if (CastForSlopeNormal(out slopeNormal))
            {
                return true;
            }
            return false;
        }

        public static bool IsSlideableAngle(float slopeAngle, float slopeLimit)
        {
            return slopeLimit <= slopeAngle && slopeAngle < k_MaxSlideAngle;
        }

        
        public static float CalculateSlideVerticalVelocity(Vector3 slopeNormal, float slidingTime, float slideGravityMultiplier, float slideMaxSpeed)
        {
            float slopeAngle = Vector3.Angle(Vector3.up, slopeNormal);
            float slideSpeedScale = Mathf.Clamp01(slopeAngle / k_MaxSlideAngle);
            float gravity = Mathf.Abs(Physics.gravity.y) * slideGravityMultiplier * slideSpeedScale;
            return -Mathf.Clamp(gravity * slidingTime, 0, Mathf.Abs(slideMaxSpeed));
        }        
        
        bool DoSlideMove(Vector3 slopeNormal, float slidingTimeElapsed)
        {
            float velocityY = CalculateSlideVerticalVelocity(slopeNormal, slidingTimeElapsed, slideGravityMultiplier, slideMaxSpeed);
            velocityY *= Time.deltaTime;
            Vector3 push = new Vector3(slopeNormal.x, 0, slopeNormal.z).normalized * k_PushAwayFromSlopeDistance;
            Vector3 moveVector = new Vector3(push.x, velocityY, push.z);
            CollisionFlags oldCollisionFlags = collisionFlags;
            Vector3 oldVelocity = velocity;
            bool didSlide = true;
            MoveInternal(moveVector, true, true, true);
            if ((collisionFlags & CollisionFlags.CollidedSides) != 0)
            {
                didSlide = false;
            }
            collisionFlags = oldCollisionFlags;
            velocity = oldVelocity;
            return didSlide;
        }
        
        SlidingState UpdateSlidingNONE()
        {
            if (ReuseOrCastForSlopeNormal(out Vector3 slopeNormal) &&
                IsSlideableAngle(Vector3.Angle(Vector3.up, slopeNormal), slopeLimit))
            {
                slidingStartedTime = Time.time;
                return SlidingState.STARTING;
            }
            else return SlidingState.NONE;
        }

        
        SlidingState UpdateSlidingSTARTING()
        {
            if (ReuseOrCastForSlopeNormal(out Vector3 slopeNormal) &&
                IsSlideableAngle(Vector3.Angle(Vector3.up, slopeNormal), slopeLimit))
            {
                if (Time.time >= slidingStartedTime + slideStartDelay)
                {
                    return SlidingState.SLIDING;
                }
                else return SlidingState.STARTING;
            }
            else
            {
                return SlidingState.NONE;
            }
        }

        
        SlidingState UpdateSlidingSLIDING()
        {
            if (ReuseOrCastForSlopeNormal(out Vector3 slopeNormal) &&
                IsSlideableAngle(Vector3.Angle(Vector3.up, slopeNormal), slopeLimit))
            {
                float slidingTimeElapsed = Time.time - slidingStartedTime;
                if (DoSlideMove(slopeNormal, slidingTimeElapsed))
                {
                    return SlidingState.SLIDING;
                }
                else
                {
                    slidingStoppedTime = Time.time;
                    return SlidingState.STOPPING;
                }
            }
            else
            {
                slidingStoppedTime = Time.time;
                return SlidingState.STOPPING;
            }
        }
        
        SlidingState UpdateSlidingSTOPPING()
        {
            if (ReuseOrCastForSlopeNormal(out Vector3 slopeNormal) &&
                IsSlideableAngle(Vector3.Angle(Vector3.up, slopeNormal), slopeLimit))
            {
                return SlidingState.SLIDING;
            }
            else if (Time.time >= slidingStoppedTime + slideStopDelay)
            {
                
                return SlidingState.NONE;
            }
            else
            {
                return SlidingState.STOPPING;
            }
        }

        
        void UpdateSlideDownSlopes()
        {
            
            if (!slideDownSlopes || !isGrounded)
            {
                slidingState = SlidingState.NONE;
                return;
            }            
            if      (slidingState == SlidingState.NONE)     slidingState = UpdateSlidingNONE();
            else if (slidingState == SlidingState.STARTING) slidingState = UpdateSlidingSTARTING();
            else if (slidingState == SlidingState.SLIDING)  slidingState = UpdateSlidingSLIDING();
            else if (slidingState == SlidingState.STOPPING) slidingState = UpdateSlidingSTOPPING();
            else Debug.LogError("Unhandled sliding state: " + slidingState);
        }

        void SetRootToOffset()
        {
            if (playerRootTransform != null)
            {
                playerRootTransform.localPosition = rootTransformOffset;
            }
        }
    }
}