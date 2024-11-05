using Microsoft.AspNetCore.Components;
using SkiaSharp;

namespace BlazorApp.Components.Shared;
public class PixelMapBase : ComponentBase {

  [Parameter]
  public int Width { get; set; }
  [Parameter]
  public int Height { get; set; }
  public string Source { get; set; }

  private Timer nextTimer;

  private SKBitmap bitmap;

  private async Task Update() {
    Stream bitmapStream = bitmap.Encode(SKEncodedImageFormat.Png, 100).AsStream();
    Stream outStream = File.Create("wwwroot/images/output.png");

    bitmapStream.CopyTo(outStream);
    Source = $"images/output.png?Dummy={DateTime.Now}";

    bitmapStream.Close(); outStream.Close();
    bitmap.Dispose();

    await InvokeAsync(StateHasChanged);
    Console.WriteLine("Update performed");
  }

  protected override async void OnInitialized() {
    bitmap = new SKBitmap(Width, Height);
    Source = "images/output.png";

    Console.WriteLine("Init");
    await Update();

    //nextTimer = new Timer(2000);
    // Hook up the Elapsed event for the timer. 
    //nextTimer.Elapsed += Next;
    //nextTimer.AutoReset = true;
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

    //nextEnabled = true;
  }

  public async Task Next() {
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
}