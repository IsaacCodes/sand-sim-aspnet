using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using SkiaSharp;

namespace BlazorApp.Components.Shared;
public class PixelMapBase : ComponentBase {

  [Parameter]
  public int Width { get; set; }
  [Parameter]
  public int Height { get; set; }

  [Parameter]
  public int Scale { get; set; }

  [Parameter]
  public int NextDelay { get; set; }
  [Parameter]
  public int ClickDelay { get; set; }
  [Parameter]
  public EventCallback<MouseEventArgs> OnMouseDownCallback { get; set; }
  [Parameter]
  public EventCallback<MouseEventArgs> OnMouseUpCallback { get; set; }
  [Parameter]
  public EventCallback<MouseEventArgs> OnMouseMoveCallback { get; set; }

  public string Source { get; set; }

  public bool isClicking = false;
  public bool gravity = true;
  public int clickRadius = 3;
  public int particle = 1;
  public MouseEventArgs mouseArgs;

  private SKBitmap bitmap;
  private SKCanvas canvas;
  private Random random;
  private PeriodicTimer nextTimer;
  private PeriodicTimer clickTimer;

  private enum Particle { bg, defualt, stone, sand, sky };
  private SKColor[] pColors = { SKColors.Empty, SKColors.Red, SKColors.Gray, SKColors.Yellow, SKColors.SkyBlue };
  private SKPaint[] pPaints;


  protected override async void OnInitialized() {
    //Initializes bitmap and updates image
    bitmap = new SKBitmap(Width, Height);
    Source = "images/output.png";
    await Update();

    //Array of paints created from colors
    pPaints = new SKPaint[pColors.Length];
    
    for(int i = 0; i < pPaints.Length; i++) {
      pPaints[i] = new SKPaint {
        IsAntialias = false,
        Color = pColors[i],
        StrokeCap = SKStrokeCap.Round,
        BlendMode = SKBlendMode.Src
      };
    }

    //Canvas, timer, and random objects
    canvas = new SKCanvas(bitmap);

    nextTimer = new PeriodicTimer(TimeSpan.FromMilliseconds(NextDelay));
    clickTimer = new PeriodicTimer(TimeSpan.FromMilliseconds(ClickDelay));
    random = new Random();

    //Start main loop, ignore warnings
    Task.Run(NextClock);
    Task.Run(ClickClock);

    Console.WriteLine("Initialized");
  }

  private async Task Update() {
    Stream bitmapStream = bitmap.Encode(SKEncodedImageFormat.Png, 100).AsStream();
    FileStream outStream = new FileStream("wwwroot/images/output.png", FileMode.Create, FileAccess.Write, FileShare.Read);

    await bitmapStream.CopyToAsync(outStream);
    Source = $"images/output.png?Dummy={DateTime.Now.Ticks}";

    bitmapStream.Close();
    outStream.Close();

    await InvokeAsync(StateHasChanged);

    //Console.WriteLine("Updated\n");
  }


  public async Task Generate() {

    SKColor bg = pColors[(int) Particle.bg];
    SKColor fill = pColors[(int) Particle.defualt];

    for(int x = 0; x < Width; x++) {
      for(int y = 0; y < Height; y++) {

        if (random.Next(0, 100) < 10 && bitmap.GetPixel(x, y) == bg) {
          bitmap.SetPixel(x, y, fill);
        }

      }
    }

    //Console.WriteLine("Generated");
    await Update();
  }

  public void Clear() {
    bitmap.Erase(pColors[(int) Particle.bg]);
  }

  private async Task NextBitmap() {

    if (!gravity) {
      await Update();
      return;
    }

    SKColor bg = pColors[(int) Particle.bg];

    for(int x = 0; x < Width; x++) {
      for(int y = Height-2; y >= 0; y--) {
        SKColor here = bitmap.GetPixel(x, y);
        SKColor below = bitmap.GetPixel(x, y+1);

        if(here != bg && below == bg) {
          bitmap.SetPixel(x, y, bg);
          bitmap.SetPixel(x, y+1, here);
        }
      }
    }
    await Update();
  }

  private async Task NextClock() {
    while (await nextTimer.WaitForNextTickAsync()) {
      DateTime startTime = DateTime.Now;
      
      await NextBitmap();

      DateTime endTime = DateTime.Now;
      //Console.WriteLine($"Next time: {endTime.Subtract(startTime).Milliseconds} ms");
    }
  }

  private async Task ClickClock() {
    while (await clickTimer.WaitForNextTickAsync()) {
      if(isClicking) {
        Click();
      }
    }
  }

  public void Click() {

    float x = (float) Math.Round(mouseArgs.OffsetX/Scale);
    float y = (float) Math.Round(mouseArgs.OffsetY/Scale);

    SKPaint paint;
    if (mouseArgs.ShiftKey) {
      paint = pPaints[(int) Particle.bg];
    }
    else {
      paint = pPaints[particle];
    }

    canvas.DrawCircle(x, y, clickRadius, paint);

    //Console.WriteLine($"Clicked at: {x}, {y}");
  }

  public void Dispose() {
    Console.WriteLine("Disposed");
    nextTimer.Dispose();
    nextTimer = null;
    clickTimer.Dispose();
    clickTimer = null;
    canvas.Dispose();
    canvas = null;
    bitmap.Dispose();
    bitmap = null;
  }
}