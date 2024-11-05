using Microsoft.AspNetCore.Components;
using SkiaSharp;

namespace BlazorApp.Components.Shared;
public class PixelMapBase : ComponentBase {

  [Parameter]
  public int Width { get; set; }
  [Parameter]
  public int Height { get; set; }
  [Parameter] 
  public string Source { get; set; }

  private async Task Update(SKBitmap bitmap) {
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
    SKBitmap bitmap = new SKBitmap(Width, Height);
    
    Source = "images/output.png";

    Console.WriteLine("Init");
    await Update(bitmap);
  }

  public async Task Generate() {
    SKBitmap bitmap = new SKBitmap(Width, Height);

    Random random = new Random();
    byte r = (byte) random.Next(0, 256);
    byte g = (byte) random.Next(0, 256);
    byte b = (byte) random.Next(0, 256);

    SKColor fillColor = new SKColor(r, g, b, 255);

    for(int x = 0; x < Width; x++) {
      for(int y = 0; y < Height; y++) {

        if (x > y) {
          bitmap.SetPixel(x, y, fillColor);
        }

      }
    }

    Console.WriteLine("Generated");
    await Update(bitmap);
  }
}