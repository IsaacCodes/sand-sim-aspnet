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
  public bool isClicking;
  public int clickRadius = 3;
  public MouseEventArgs mouseArgs;

  private SKBitmap bitmap;
  private SKCanvas canvas;
  private Random random;
  private PeriodicTimer nextTimer;
  private PeriodicTimer clickTimer;

  protected override async void OnInitialized() {
    bitmap = new SKBitmap(Width, Height);
    Source = "images/output.png";
    await Update();

    canvas = new SKCanvas(bitmap);

    nextTimer = new PeriodicTimer(TimeSpan.FromMilliseconds(NextDelay));
    clickTimer = new PeriodicTimer(TimeSpan.FromMilliseconds(ClickDelay));
    random = new Random();

    NextClock();
    ClickClock();

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
    bitmap.Erase(SKColor.Empty);

    byte r = (byte) random.Next(0, 256);
    byte g = (byte) random.Next(0, 256);
    byte b = (byte) random.Next(0, 256);

    SKColor fillColor = new SKColor(r, g, b);

    for(int x = 0; x < Width; x++) {
      for(int y = 0; y < Height; y++) {

        if (random.Next(0, 100) < 10) {
          bitmap.SetPixel(x, y, fillColor);
        }

      }
    }

    //Console.WriteLine("Generated");
    await Update();
  }

  private async Task NextBitmap() {
    SKColor bg = SKColors.Empty;

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

    SKColor color;
    if (mouseArgs.ShiftKey) {
      color = SKColors.Empty;
    }
    else {
      color = new SKColor(255, 0, 0);
    }

    SKPaint paint = new SKPaint {
      IsAntialias = false,
      Color = color,
      StrokeCap = SKStrokeCap.Round,
      BlendMode = SKBlendMode.Src
    };

    canvas.DrawCircle(x, y, clickRadius, paint);

    //Console.WriteLine($"Clicked at: {x}, {y}");
  }

  public void DeepDispose() {
    Console.WriteLine("Disposed");
    nextTimer.Dispose();
    nextTimer = null;
    clickTimer.Dispose();
    clickTimer = null;
    canvas.Dispose();
    bitmap.Dispose();
  }
}