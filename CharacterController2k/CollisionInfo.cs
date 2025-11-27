using UnityEngine;

namespace Controller2k
{
    
    public struct CollisionInfo
    {
        
        public readonly Collider collider;

        
        public readonly CharacterController2k controller;

        
        public readonly GameObject gameObject;

        
        public readonly Vector3 moveDirection;

        
        public readonly float moveLength;

        
        public readonly Vector3 normal;

        
        public readonly Vector3 point;

        
        public readonly Rigidbody rigidbody;

        
        public readonly Transform transform;

        
        
        
        
        
        public CollisionInfo(CharacterController2k openCharacterController,
                             RaycastHit hitInfo,
                             Vector3 directionMoved,
                             float distanceMoved)
        {
            collider = hitInfo.collider;
            controller = openCharacterController;
            gameObject = hitInfo.collider.gameObject;
            moveDirection = directionMoved;
            moveLength = distanceMoved;
            normal = hitInfo.normal;
            point = hitInfo.point;
            rigidbody = hitInfo.rigidbody;
            transform = hitInfo.transform;
        }
    }
}