using UnityEngine;

[System.Serializable]
public struct CurveAngles
{
    [SerializeField] float angle0;
    [SerializeField] float angle1;
    
    public float this[int index]
    {
        get
        {
            return index switch
            {
                0 => angle0,
                1 => angle1,
                _ => throw new System.IndexOutOfRangeException("Index cannot be lower than 0 or higher than one.")
            };
        }
        set
        {
            switch (index)
            {
                case 0: angle0 = value; return;
                case 1: angle1 = value; return;
                default: throw new System.IndexOutOfRangeException("Index cannot be lower than 0 or higher than one.");
            }
        }
    }
    
    public CurveAngles(float firstAngle, float secondAngle)
    {
        angle0 = firstAngle;
        angle1 = secondAngle;
    }
}
