using Qualia.Tools;
using System;

namespace Qualia.Controls
{
    sealed public class FakeValue : IConfigParam
    {
        public Notification.ParameterChanged UIParam { get; private set; }

        public FakeValue(Notification.ParameterChanged param)// = Notification.ParameterChanged.Fake)
        {
            UIParam = param;
        }

        // IConfigParam

        public void SetConfig(Config config)
        {
            //
        }

        public void LoadConfig()
        {
            //
        }

        public void SaveConfig()
        {
            //
        }

        public void RemoveFromConfig()
        {
            //
        }

        public bool IsValid() => true;

        public void SetOnChangeEvent(Action<Notification.ParameterChanged> onChanged)
        {
            //
        }

        public void InvalidateValue()
        {
            //
        }

        //
    }
}
