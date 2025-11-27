using UnityEngine;

namespace Controller2k
{
    
    public class StuckInfo
    {
        
        Vector3? stuckPosition;

        
        int stuckPositionCount;

        
        const float k_StuckDistance = 0.001f;

        
        const int k_HitCountForStuck = 6;

        
        const int k_MaxStuckPositionCount = 1;

        
        public bool isStuck;

        
        public int hitCount;

        
        public void OnMoveLoop()
        {
            hitCount = 0;
            stuckPositionCount = 0;
            stuckPosition = null;
            isStuck = false;
        }

        
        
        
        
        public bool UpdateStuck(Vector3 characterPosition, Vector3 currentMoveVector,
                                Vector3 originalMoveVector)
        {
            
            if (!isStuck)
            {
                
                if (currentMoveVector.sqrMagnitude.NotEqualToZero() &&
                    Vector3.Dot(currentMoveVector, originalMoveVector) <= 0.0f)
                {
                    isStuck = true;
                }
            }

            
            if (!isStuck)
            {
                
                if (hitCount < k_HitCountForStuck)
                {
                    return false;
                }

                if (stuckPosition == null)
                {
                    stuckPosition = characterPosition;
                }
                else if (Vector3.Distance(stuckPosition.Value, characterPosition) <= k_StuckDistance)
                {
                    stuckPositionCount++;
                    if (stuckPositionCount > k_MaxStuckPositionCount)
                    {
                        isStuck = true;
                    }
                }
                else
                {
                    stuckPositionCount = 0;
                    stuckPosition = null;
                }
            }

            if (isStuck)
            {
                isStuck = false;
                hitCount = 0;
                stuckPositionCount = 0;
                stuckPosition = null;

                return true;
            }

            return false;
        }
    }
}