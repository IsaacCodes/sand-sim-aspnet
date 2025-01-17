using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using SkiaSharp;


namespace BlazorAppWasm.Shared;
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

  public bool isClicking;
  public MouseEventArgs mouseArgs;

  private SKBitmap bitmap;
  private SKCanvas canvas;
  private Random random;
  private PeriodicTimer nextTimer;
  private PeriodicTimer clickTimer;

  [Inject]
  private IJSRuntime JS { get; set; }

  private IJSObjectReference JSModule;


  protected override async void OnInitialized() {

    JSModule = await JS.InvokeAsync<IJSObjectReference>("import", "./scripts/imageHandler.js");

    bitmap = new SKBitmap(Width, Height);
    canvas = new SKCanvas(bitmap);
    await Update();

    nextTimer = new PeriodicTimer(TimeSpan.FromMilliseconds(NextDelay));
    clickTimer = new PeriodicTimer(TimeSpan.FromMilliseconds(ClickDelay));
    random = new Random();

    NextClock();
    ClickClock();

    Console.WriteLine("Initialized");
  }

  private async Task Update() {
    Stream bitmapStream = bitmap.Encode(SKEncodedImageFormat.Png, 100).AsStream();
    DotNetStreamReference streamRef = new DotNetStreamReference(bitmapStream);
    
    await JSModule.InvokeVoidAsync("setImage", "PixelMap", streamRef);
    bitmapStream.Close();

    await InvokeAsync(StateHasChanged);

    //Console.WriteLine("Updated");
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

      DateTime endTime = DateTime.Now;
      Console.WriteLine($"Next time: {endTime.Subtract(startTime).Milliseconds} ms");
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

    int radius = 3;
    SKColor color;
    
    if (mouseArgs.ShiftKey) {
      color = SKColor.Empty;
    }
    else {
      color = new SKColor(255, 0, 0);
    }

    SKPaint paint = new SKPaint {
      IsAntialias = false,
      Color = color,
      StrokeCap = SKStrokeCap.Round
    };

    canvas.DrawCircle(x, y, radius, paint);

    Console.WriteLine($"Clicked at: {x}, {y}");
  }
}