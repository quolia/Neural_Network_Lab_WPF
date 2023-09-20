using System;
using System.Runtime.CompilerServices;

namespace Qualia.Tools;

public static unsafe class UnsafeTools
{
    public static IntPtr AddressOf<T>(ref T t)
    {
        var tr = __makeref(t);
        return *(IntPtr*)&tr;
    }
}
public unsafe struct Pointer<T>
{
    private readonly void* _value;

    public Pointer(void* v)
    {
        _value = v;
    }

    public T Value
    {
        get => Unsafe.Read<T>(_value);
        set => Unsafe.Write(_value, value);
    }

    public static implicit operator Pointer<T>(void* v)
    {
        return new(v);
    }

    public static implicit operator Pointer<T>(IntPtr p)
    {
        return new(p.ToPointer());
    }
}

public unsafe class Ref : ListXNode<Ref>
{
    private readonly Pointer<double> _ptr;

    public Ref(ref double val)
    {
        _ptr = UnsafeTools.AddressOf(ref val);
    }

    public double Value => _ptr.Value;
}