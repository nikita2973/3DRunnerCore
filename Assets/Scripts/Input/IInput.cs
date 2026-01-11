using System;

namespace Input
{
    public interface IInput
    {
        public void SetDataGetter(InputData inputData);
        public void Initialize();
    }
    
    public record InputData
    {
        public float X, Y;
        public bool Running,Jumping;
        public Action Jump,Attack;
    }
}