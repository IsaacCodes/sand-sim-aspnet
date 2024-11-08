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
  public int Delay { get; set; }
  [Parameter]
  public EventCallback<MouseEventArgs> OnClickCallback { get; set; }

  public string Source { get; set; }

  private SKBitmap bitmap;
  private SKCanvas canvas;
  private Random random;
  private PeriodicTimer nextTimer;

  protected override async void OnInitialized() {
    bitmap = new SKBitmap(Width, Height);
    Source = "images/output.png";
    await Update();

    canvas = new SKCanvas(bitmap);

    nextTimer = new PeriodicTimer(TimeSpan.FromMilliseconds(Delay));
    random = new Random();

    NextClock();

    Console.WriteLine("Initialized");
  }

  private async Task Update() {
    Console.WriteLine("Start update");


    //error atm. look at https://learn.microsoft.com/en-us/dotnet/csharp/asynchronous-programming/using-async-for-file-access
    //https://stackoverflow.com/questions/11774827/writing-to-a-file-asynchronously
    Stream bitmapStream = bitmap.Encode(SKEncodedImageFormat.Png, 100).AsStream();
     outStream = File.OpenRead

    Console.WriteLine("Opened file");

    outStream.
    Source = $"images/output.png?Dummy={DateTime.Now}";

    Console.WriteLine("Copied to file");

    bitmapStream.Close();
    outStream.Close();

    await InvokeAsync(StateHasChanged);

    Console.WriteLine("Updated\n");
  }


  public async Task Generate() {

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

  private async Task NextClock() {
    while (await nextTimer.WaitForNextTickAsync()) {
      DateTime startTime = DateTime.Now;

      await NextBitmap();
      await InvokeAsync(StateHasChanged);

      DateTime endTime = DateTime.Now;
      //Console.WriteLine($"Next time: {endTime.Subtract(startTime).Milliseconds} ms");
    }
  }

  public void Click(MouseEventArgs args) {

    float x = (float) Math.Round(args.OffsetX/Scale);
    float y = (float) Math.Round(args.OffsetY/Scale);

    int radius = 3;

    SKPaint paint = new SKPaint {
      IsAntialias = false,
      Color = new SKColor(255, 0, 0),
      StrokeCap = SKStrokeCap.Round
    };

    canvas.DrawCircle(x, y, radius, paint);

    Console.WriteLine($"Clicked at: {x}, {y}");
  }
}