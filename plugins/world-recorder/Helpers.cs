using System;
using System.IO;
using System.Threading;
using UnityEngine;

internal static class BmpEncoder {
    private static readonly byte[][] _paddingTable = [
        [], [0], [0, 0], [0, 0, 0]
    ];

    public delegate Color32 GetPixelDelegate(uint x, uint y);

    public static void WriteBmp(Stream stream, uint width, uint height, GetPixelDelegate getColor) {
        using var writer = new BinaryWriter(stream);
        uint length = width * height * 3;

        // BM signature
        writer.Write((ushort)0x4D42);
        // File size
        writer.Write((uint)(40 + 14 + length));
        // Reserved
        writer.Write((uint)0);
        // Pixel data offset
        writer.Write((uint)54);
        // Header size
        writer.Write((uint)40);
        // Image width
        writer.Write((uint)width);
        // Image height
        writer.Write((uint)height);
        // Planes
        writer.Write((ushort)1);
        // Bits per pixel (RGB24)
        writer.Write((ushort)24);
        // Compression (none)
        writer.Write((uint)0);
        // Image size (can be 0 for uncompressed)
        writer.Write((uint)0);
        // Horizontal resolution (pixels/meter)
        writer.Write((uint)0);
        // Vertical resolution (pixels/meter)
        writer.Write((uint)0);
        // Colors in palette
        writer.Write((uint)0);
        // Important colors
        writer.Write((uint)0);

        byte[] padding = _paddingTable[(4 - ((width * 3) % 4)) % 4];
        // 0 -> 0, 1 -> 3, 2 -> 2, 3 -> 1

        for (long y = height - 1; y >= 0; --y) {
            for (uint x = 0; x < width; ++x) {
                Color32 color = getColor(x, (uint)y);
                writer.Write(color.b);
                writer.Write(color.g);
                writer.Write(color.r);
            }

            writer.Write(padding);
        }
    }
}

internal sealed class SingleThreadedWorker : IDisposable {
    private readonly Thread _worker;
    private readonly AutoResetEvent _trigger = new(initialState: false);
    private readonly ManualResetEvent _completed = new(initialState: true);

    private readonly Action _task;
    private volatile bool _running = true;
    private bool _disposed = false;

    public SingleThreadedWorker(Action task) {
        _task = task ?? throw new ArgumentNullException(nameof(task));
        _worker = new Thread(WorkerLoop) { IsBackground = true };
        _worker.Start();
    }

    public void Run() {
        ThrowIfDisposed();

        _completed.Reset();
        _trigger.Set();
    }

    public void Wait() {
        ThrowIfDisposed();

        _completed.WaitOne();
    }

    private void WorkerLoop() {
        while (true) {
            _trigger.WaitOne();
            if (!_running) { break; }

            try {
                _task();
            } finally {
                _completed.Set();
            }
        }
    }

    public void Dispose() {
        if (_disposed) { return; }
        _disposed = true;

        _running = false;
        _trigger.Set();

        if (!_worker.Join(500)) {
            _worker.Interrupt();
            _worker.Join(100);
        }

        _trigger.Close();
        _completed.Close();
    }

    private void ThrowIfDisposed() {
        if (_disposed) {
            throw new ObjectDisposedException(nameof(SingleThreadedWorker));
        }
    }
}
