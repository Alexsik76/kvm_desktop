using Avalonia.Input;

namespace KvmDesktop.Services;

public interface IEventMapper
{
    byte AvaloniaToHidKey(Key key);
    byte AvaloniaToHidModifiers(KeyModifiers modifiers);
}
