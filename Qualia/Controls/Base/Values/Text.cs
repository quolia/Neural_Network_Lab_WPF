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
            Margin = new(0);
            Padding = new(3, 3, 0, 3);
        }
    }
}
