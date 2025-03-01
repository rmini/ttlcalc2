using System;
using System.Collections.Generic;

namespace TtlCalcSim;

public class InputOutput
{
    private readonly List<(byte startAddr, byte endAddr, Func<byte, Nybble> handlerFunc)> _inputHandlers = [];
    private readonly List<(byte startAddr, byte endAddr, Action<byte, Nybble> handlerFunc)> _outputHandlers = [];

    public IReadOnlyList<(byte startAddr, byte endAddr, Func<byte, Nybble> handlerFunc)> InputHandlers => _inputHandlers;
    public IReadOnlyList<(byte startAddr, byte endAddr, Action<byte, Nybble> handlerFunc)> OutputHandlers => _outputHandlers;

    public Func<byte, Nybble>? DefaultInputHandler { get; set; } = null;

    public virtual Nybble Read(byte addr)
    {
        foreach (var handler in _inputHandlers)
        {
            byte startAddr = handler.startAddr;
            byte endAddr = handler.endAddr;

            if (addr >= startAddr && addr <= endAddr)
            {
                return handler.handlerFunc(addr);
            }
        }

        return DefaultInputHandler?.Invoke(addr) ?? throw new InvalidOperationException("No input handler found for the given address.");
    }

    public virtual void Write(byte addr, Nybble d)
    {
        foreach (var handler in _outputHandlers)
        {
            byte startAddr = handler.startAddr;
            byte endAddr = handler.endAddr;
            if (addr >= startAddr && addr <= endAddr)
            {
                handler.handlerFunc(addr, d);
            }
        }
    }

    public void AddInputHandler(byte startAddr, byte endAddr, Func<byte, Nybble> handlerFunc)
    {
        foreach (var handler in _inputHandlers)
        {
            byte existingStartAddr = handler.startAddr;
            byte existingEndAddr = handler.endAddr;

            if ((startAddr >= existingStartAddr && startAddr <= existingEndAddr) ||
                (endAddr >= existingStartAddr && endAddr <= existingEndAddr) ||
                (existingStartAddr >= startAddr && existingStartAddr <= endAddr))
            {
                throw new InvalidOperationException("An overlapping input handler already exists.");
            }
        }

        _inputHandlers.Add((startAddr, endAddr, handlerFunc));
    }

    public void AddOutputHandler(byte startAddr, byte endAddr, Action<byte, Nybble> handlerFunc)
    {
        _outputHandlers.Add((startAddr, endAddr, handlerFunc));
    }

    public void RemoveInputHandler(int index)
    {
        if (index < 0 || index >= _inputHandlers.Count)
            throw new ArgumentOutOfRangeException(nameof(index), "Index is out of range.");
        
        _inputHandlers.RemoveAt(index);
    }

    public void RemoveOutputHandler(int index)
    {
        if (index < 0 || index >= _outputHandlers.Count)
            throw new ArgumentOutOfRangeException(nameof(index), "Index is out of range.");
        
        _outputHandlers.RemoveAt(index);
    }
}
