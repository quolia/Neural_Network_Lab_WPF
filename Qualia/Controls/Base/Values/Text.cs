using System.Windows.Controls;

namespace Qualia.Controls
{
    sealed public class TextControl : Label
    {
        public string Text
        {
            get => Content as string;
            set => Content = value;
        }

        public TextControl()
        {
            //Padding = new(3);
            Margin = new(0, 0, 0, 0);
            Padding = new(3, 3, 0, 3);
        }
    }
}
