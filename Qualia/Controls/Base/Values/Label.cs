using System.Windows.Controls;

namespace Qualia.Controls
{
    sealed public class LabelControl : Label
    {
        public string Text
        {
            get => Content as string;
            set => Content = value;
        }

        public LabelControl()
        {
            //Padding = new(3);
            Margin = new(0);
        }
    }
}
