using System.Windows.Input;

namespace Folminder.Models
{
    public record HotKey(bool Alt, bool Control, bool Shift, bool Win, Key Key)
    {
        public static HotKey Default { get; } = new HotKey(Alt: true, Control: true, Shift: false, Win: false, Key.F);
    }
}
