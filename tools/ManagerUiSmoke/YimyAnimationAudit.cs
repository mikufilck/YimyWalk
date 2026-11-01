using System.Numerics;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using YimyWalk.App.Host;
using YimyWalk.App.Rendering;
using YimyWalk.App.Windows;
using YimyWalk.Core.Characters;
using YimyWalk.Core.Imaging;
using YimyWalk.Core.Simulation;

internal static class YimyAnimationAudit
{
    private const BindingFlags Flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

    internal static void Run()
    {
        var root = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../../../"));
        var output = Path.Combine(root, "artifacts", "yimy-animation-20261101");
        Directory.CreateDirectory(output);
        foreach (var role in new[] { "咿米_Yimy", "咿米_Line" }) InspectSource(root, output, role);
        InspectDesktop(root, output);
    }

    private static void InspectSource(string root, string output, string role)
    {
        var sourcePath = Path.Combine(root, "Characters", role, "sprite.png");
        var hash = Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(sourcePath)));
        using var definition = CharacterLoader.LoadDirectory(Path.GetDirectoryName(sourcePath)!);
        using var cache = new FrameSurfaceCache();
        using var sheet = new Bitmap(810, 885);
        using var graphics = Graphics.FromImage(sheet);
        using var font = new Font("Microsoft YaHei UI", 13, GraphicsUnit.Pixel);
        graphics.Clear(Color.FromArgb(238, 242, 247));
        var rows = new List<string> { "frame,kind,left,top,right,bottom,visible_pixels,top_edge_pixels,bottom_edge_pixels,rgba_sha256" };
        for (var i = 0; i < 9; i++)
        {
            var frame = definition.Frames[i];
            var pixels = frame.RgbaPixels.Span;
            using var copy = new PngImage(frame.Width, frame.Height, default);
            var left = frame.Width; var top = frame.Height; var right = -1; var bottom = -1;
            var visible = 0; var topEdge = 0; var bottomEdge = 0;
            for (var y = 0; y < frame.Height; y++)
            for (var x = 0; x < frame.Width; x++)
            {
                var p = (y * frame.Width + x) * 4;
                copy[x, y] = new Rgba32(pixels[p], pixels[p + 1], pixels[p + 2], pixels[p + 3]);
                if (pixels[p + 3] < 16) continue;
                left = Math.Min(left, x); top = Math.Min(top, y); right = Math.Max(right, x); bottom = Math.Max(bottom, y);
                visible++; if (y == 0) topEdge++; if (y == frame.Height - 1) bottomEdge++;
            }
            Check(visible > 0, role + " frame " + i + " contains visible artwork");
            copy.SaveAsPng(Path.Combine(output, role + "-source-" + i + ".png"));
            rows.Add($"{i},{frame.Kind},{left},{top},{right},{bottom},{visible},{topEdge},{bottomEdge},{Convert.ToHexString(SHA256.HashData(pixels))}");
            using var rendered = cache.Acquire(definition, frame, false, .58f);
            graphics.DrawImageUnscaled(rendered.Surface.Bitmap, i % 3 * 270 + 14, i / 3 * 295 + 8);
            graphics.DrawString($"#{i} {frame.Kind}   y={top}..{bottom}", font, Brushes.DarkSlateGray, i % 3 * 270 + 14, i / 3 * 295 + 254);
            graphics.DrawString($"edge top={topEdge} / bottom={bottomEdge}", font, Brushes.DarkSlateGray, i % 3 * 270 + 14, i / 3 * 295 + 274);
        }
        File.WriteAllLines(Path.Combine(output, role + "-frames.csv"), rows, Encoding.UTF8);
        sheet.Save(Path.Combine(output, role + "-frames.png"));
        Console.WriteLine($"SOURCE ROLE={role} FRAMES=9 SHA256={hash} ANCHOR={definition.Anchor} IDLE={string.Join(',', definition.Animation.IdleFrames)} WALK={string.Join(',', definition.Animation.WalkFrames)} STRIDE={definition.Animation.WalkStridePixels} SAMPLING={definition.Sampling}");
    }

    private static void InspectDesktop(string root, string output)
    {
        var sandbox = Path.Combine(Path.GetTempPath(), "YimyWalk-animation-" + Guid.NewGuid().ToString("N"));
        var roleRoot = Path.Combine(sandbox, "Characters");
        Directory.CreateDirectory(roleRoot);
        try
        {
            foreach (var role in new[] { "咿米_Yimy", "咿米_Line" })
            {
                var target = Path.Combine(roleRoot, role);
                Directory.CreateDirectory(target);
                foreach (var file in Directory.EnumerateFiles(Path.Combine(root, "Characters", role)))
                    File.Copy(file, Path.Combine(target, Path.GetFileName(file)));
            }
            var area = Screen.PrimaryScreen!.WorkingArea;
            using var backdrop = new Form
            {
                FormBorderStyle = FormBorderStyle.None, AutoScaleMode = AutoScaleMode.None,
                StartPosition = FormStartPosition.Manual, ShowInTaskbar = false, TopMost = true,
                Bounds = new Rectangle(area.Left + 60, area.Bottom - 480, 960, 480),
                BackColor = Color.FromArgb(220, 233, 244)
            };
            backdrop.Show();
            using var host = new AppHost(Path.Combine(sandbox, "settings"));
            host.CharacterRootOverride = roleRoot;
            host.Start();
            ((System.Windows.Forms.Timer)Field(host, "_timer")).Stop();
            host.GlobalScale = .3f;
            host.DialogueEnabled = false;
            host.JumpDesire = 0; host.ClimbDesire = 0; host.WindowJumpDesire = 0;
            var session = (PetSession)Field(host, "_session");
            var registry = (PetWindowRegistry)Field(host, "_windows");
            var trace = new List<string> { "role,phase,step,state,frame,mirrored,x,y,vy,animation_seconds" };
            foreach (var role in new[] { "咿米_Yimy", "咿米_Line" })
            {
                var pet = session.Create(role, new Vector2(area.Left + 320, area.Bottom));
                Check(registry.TryGet(pet.InstanceId, out var found) && found is not null, "real pet window exists");
                var window = found!;
                void Step(string phase, int step)
                {
                    session.Tick(1d / 60);
                    window.RefreshFrame();
                    var surface = (RenderedSurface)Field(window, "_surface");
                    var expected = pet.Definition.SelectFrame(pet.GetFrameKind(), pet.AnimationTime);
                    Check(surface.FrameIndex == expected.Index, "presented source frame agrees with state / phase");
                    Check(!surface.IsDisposed, "presented frame stays alive");
                    trace.Add(FormattableString.Invariant($"{role},{phase},{step},{pet.State},{surface.FrameIndex},{expected.Mirrored},{pet.FootPosition.X},{pet.FootPosition.Y},{pet.Velocity.Y},{pet.AnimationTime}"));
                    Pump(17);
                }
                Step("land", 0);
                Check(pet.State == PetState.Idle, "lands on actual display floor");
                Check(window.Bottom <= area.Bottom + 1, "idle shoes are not under the taskbar");
                foreach (var direction in new[] { FacingDirection.Right, FacingDirection.Left })
                {
                    pet.StartWalking(direction);
                    var sequence = new List<int>();
                    for (var i = 0; i < 90; i++)
                    {
                        Step("walk-" + direction, i);
                        var frame = ((RenderedSurface)Field(window, "_surface")).FrameIndex;
                        if (sequence.Count == 0 || sequence[^1] != frame) sequence.Add(frame);
                        Check(window.Bottom <= area.Bottom + 1, "walking shoes stay within work area");
                    }
                    Check(sequence.Take(5).SequenceEqual(new[] { 3, 4, 5, 4, 3 }), "real walk returns through middle pose");
                    Console.WriteLine($"DESKTOP ROLE={role} WALK={direction} SEQUENCE={string.Join(',', sequence)} DPI={window.DeviceDpi} FOOT_CLIP=NONE");
                }
                // Controlled launch through the same Core method as autonomy;
                // no runtime code or source artwork is changed by the harness.
                typeof(PetInstance).GetMethod("StartJumpCharge", Flags)!.Invoke(pet, new object[] { .2f, 450f, 450f });
                Step("charge", 0);
                Check(((RenderedSurface)Field(window, "_surface")).FrameIndex == 6, "charge uses crouched source pose");
                var jumpFrames = new HashSet<int>();
                for (var i = 0; i < 100 && pet.State != PetState.Idle; i++)
                {
                    Step("jump", i);
                    jumpFrames.Add(((RenderedSurface)Field(window, "_surface")).FrameIndex);
                }
                Check(jumpFrames.IsSupersetOf(new[] { 6, 7, 8, 0 }), "jump presents launch, apex, fall and landing");
                Check(pet.Feed(), "feeding starts from landed pose");
                Step("eat", 0);
                Check(((RenderedSurface)Field(window, "_surface")).FrameIndex == 0, "eat fallback does not cycle expressions");
                var engine = new PetEngine(session.Surface, new FixedRandom());
                engine.BeginDrag(pet);
                engine.SetDraggedFootPosition(pet, pet.FootPosition - new Vector2(0, 80));
                Step("drag", 0);
                engine.ReleaseDrag(pet, new Vector2(0, -400));
                for (var i = 0; i < 100 && pet.State != PetState.Idle; i++) Step("release", i);
                Console.WriteLine($"DESKTOP ROLE={role} CHARGE=6 JUMP=6/7/8 LAND=0 EAT=0 DRAG_RELEASE=PASS");
                // Export only our presented RGBA frame. A frame export is a
                // diagnostic artifact, not a screenshot or desktop acceptance.
                ((RenderedSurface)Field(window, "_surface")).Bitmap.Save(Path.Combine(output, role + "-runtime-idle.png"));
                session.Remove(pet.InstanceId);
            }
            File.WriteAllLines(Path.Combine(output, "desktop-frames.csv"), trace, Encoding.UTF8);
            Console.WriteLine("ANIMATION_AUDIT PASS SINGLE_DISPLAY_ONLY");
        }
        finally { if (Directory.Exists(sandbox)) Directory.Delete(sandbox, true); }
    }

    private static object Field(object target, string name) => target.GetType().GetField(name, Flags)!.GetValue(target)!;
    private static void Check(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
    private static void Pump(int milliseconds)
    {
        var until = Environment.TickCount64 + milliseconds;
        while (Environment.TickCount64 < until) { Application.DoEvents(); Thread.Sleep(1); }
    }
    private sealed class FixedRandom : IRandomSource { public double NextDouble() => .5; public int Next(int minInclusive, int maxExclusive) => minInclusive; }
}
