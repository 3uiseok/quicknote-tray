// 양 모양 아이콘(sheep.ico) 생성기. 크기별 PNG를 ICO 컨테이너에 담는다
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;

static class MakeIcon
{
    static Bitmap DrawSheep(int n)
    {
        var bmp = new Bitmap(n, n, PixelFormat.Format32bppArgb);
        using (var g = Graphics.FromImage(bmp))
        {
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.Clear(Color.Transparent);
            float s = n / 64f;
            Action<Brush, float, float, float, float> E = (b, x, y, w, h) => g.FillEllipse(b, x * s, y * s, w * s, h * s);

            var dark = new SolidBrush(Color.FromArgb(45, 40, 48));
            var edge = new SolidBrush(Color.FromArgb(140, 140, 150));
            var wool = new SolidBrush(Color.FromArgb(250, 250, 252));
            var pink = new SolidBrush(Color.FromArgb(240, 160, 175));

            // 다리
            foreach (var x in new[] { 20f, 28f, 40f, 48f })
                g.FillRectangle(dark, x * s, 44 * s, 5 * s, 15 * s);

            // 털 뭉치 몸통: 회색 테두리 원을 먼저 조금 크게, 그 위에 흰 원
            var puffs = new[] {
                new[] {14f,22,18}, new[] {22f,13,20}, new[] {34f,11,20}, new[] {44f,17,18},
                new[] {46f,28,16}, new[] {34f,30,20}, new[] {20f,30,20}, new[] {12f,30,14} };
            float pad = n <= 16 ? 2.6f : 1.8f;
            foreach (var p in puffs) E(edge, p[0] - pad / 2, p[1] - pad / 2, p[2] + pad, p[2] + pad);
            foreach (var p in puffs) E(wool, p[0], p[1], p[2], p[2]);
            E(wool, 20, 20, 34, 24); // 원 사이로 비치는 테두리를 덮는 가운데 채움

            // 머리(왼쪽 앞), 귀, 이마 털, 눈, 코
            E(dark, 1, 23, 10, 6);
            E(dark, 4, 22, 16, 21);
            E(wool, 6, 16, 12, 10);
            E(Brushes.White, 7, 29, 5, 5); E(dark, 8.6f, 30.6f, 2.4f, 2.4f);
            E(pink, 6, 38, 7, 3);
        }
        return bmp;
    }

    static void Main(string[] args)
    {
        string dir = args.Length > 0 ? args[0] : ".";
        var sizes = new[] { 256, 48, 32, 24, 16 };
        var data = new List<byte[]>();
        foreach (int n in sizes)
            using (var bmp = DrawSheep(n))
            using (var ms = new MemoryStream())
            {
                bmp.Save(ms, ImageFormat.Png);
                data.Add(ms.ToArray());
                if (n == 256) bmp.Save(Path.Combine(dir, "sheep-preview.png"), ImageFormat.Png);
            }

        using (var fs = File.Create(Path.Combine(dir, "sheep.ico")))
        using (var w = new BinaryWriter(fs))
        {
            w.Write((ushort)0); w.Write((ushort)1); w.Write((ushort)sizes.Length);
            int offset = 6 + 16 * sizes.Length;
            for (int i = 0; i < sizes.Length; i++)
            {
                byte d = (byte)(sizes[i] >= 256 ? 0 : sizes[i]);
                w.Write(d); w.Write(d); w.Write((byte)0); w.Write((byte)0);
                w.Write((ushort)1); w.Write((ushort)32);
                w.Write((uint)data[i].Length); w.Write((uint)offset);
                offset += data[i].Length;
            }
            foreach (var d in data) w.Write(d);
        }
        Console.WriteLine("sheep.ico OK");
    }
}
