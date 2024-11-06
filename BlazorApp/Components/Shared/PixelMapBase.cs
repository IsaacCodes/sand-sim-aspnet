using Microsoft.AspNetCore.Components;
using SkiaSharp;

namespace BlazorApp.Components.Shared;
public class PixelMapBase : ComponentBase {

  [Parameter]
  public int Width { get; set; }
  [Parameter]
  public int Height { get; set; }
  [Parameter]
  public int Delay { get; set; }

  public string Source { get; set; }

  private SKBitmap bitmap;
  private PeriodicTimer nextTimer;
  private bool timerStarted = false;

  private async Task Update() {
    Stream bitmapStream = bitmap.Encode(SKEncodedImageFormat.Png, 100).AsStream();
    Stream outStream = File.Create("wwwroot/images/output.png");

    bitmapStream.CopyTo(outStream);
    Source = $"images/output.png?Dummy={DateTime.Now}";

    bitmapStream.Close();
    outStream.Close();

    await InvokeAsync(StateHasChanged);

    Console.WriteLine("Updated");
  }

  protected override async void OnInitialized() {
    bitmap = new SKBitmap(Width, Height);
    Source = "images/output.png";
    await Update();

    nextTimer = new PeriodicTimer(TimeSpan.FromMilliseconds(Delay));

    Console.WriteLine("Initialized");
  }

  public async Task Generate() {
    bitmap = new SKBitmap(Width, Height);

    Random random = new Random();
    byte r = (byte) random.Next(0, 256);
    byte g = (byte) random.Next(0, 256);
    byte b = (byte) random.Next(0, 256);

    SKColor fillColor = new SKColor(r, g, b, 255);

    for(int x = 0; x < Width; x++) {
      for(int y = 0; y < Height; y++) {

        if (random.Next(0, 100) < 10) {
          bitmap.SetPixel(x, y, fillColor);
        }

      }
    }

    Console.WriteLine("Generated");
    await Update();

    if(!timerStarted) {
      timerStarted = true;
      NextClock();
    }

  }

  private async Task NextBitmap() {
    SKColor bg = SKColor.Empty;

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

  private async void NextClock() {
    while (await nextTimer.WaitForNextTickAsync()) {
      DateTime startTime = DateTime.Now;

      await NextBitmap();
      await InvokeAsync(StateHasChanged);

      DateTime endTime = DateTime.Now;
      Console.WriteLine($"Next time: {endTime.Subtract(startTime).Milliseconds} ms");
    }
  }
}