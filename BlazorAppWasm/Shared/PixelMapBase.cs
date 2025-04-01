using System.Diagnostics;
using System.Runtime.CompilerServices;
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
  [Parameter]
  public EventCallback<MouseEventArgs> OnMouseLeaveCallback { get; set; }

  public string Source { get; set; }

  public bool isClicking = false;
  public bool isFrozen = false;
  public int gravityDirection = 1;
  public int clickRadius = 3;
  public int currentParticle = 1;
  public List<MouseEventArgs> mouseArgsList = new List<MouseEventArgs>();

  private SKBitmap bitmap;
  private SKCanvas canvas;
  private Random random;
  private PeriodicTimer nextTimer;
  private PeriodicTimer clickTimer;

  private enum Particle { bg, snow, stone, sand, water };
  private SKColor[] pColors = { SKColors.Empty, new SKColor(200, 240, 240), SKColors.Gray, new SKColor(250, 200, 100), SKColors.SkyBlue };
  private SKPaint[] pPaints;

  [Inject]
  private IJSRuntime JS { get; set; }
  private IJSObjectReference JSModule;

  //Initalization function on start up
  protected override async void OnInitialized() {
    //JS interop
    JSModule = await JS.InvokeAsync<IJSObjectReference>("import", "./scripts/imageHandler.js");

    //Initializes bitmap and updates image
    bitmap = new SKBitmap(Width, Height);
    canvas = new SKCanvas(bitmap);
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

    //Timers and random objects

    nextTimer = new PeriodicTimer(TimeSpan.FromMilliseconds(NextDelay));
    clickTimer = new PeriodicTimer(TimeSpan.FromMilliseconds(ClickDelay));
    random = new Random();

    //Start main loop, ignore warnings
    Task.Run(NextClock);
    Task.Run(ClickClock);

    Console.WriteLine("Initialized");
  }

  //Updates the image using the bitmap
  private async Task Update() {
    Stream bitmapStream = bitmap.Encode(SKEncodedImageFormat.Png, 100).AsStream();
    DotNetStreamReference streamRef = new DotNetStreamReference(bitmapStream);

    await JSModule.InvokeVoidAsync("setImage", "PixelMap", streamRef);
    bitmapStream.Close();

    await InvokeAsync(StateHasChanged);
  }


  //Generates a flurry of current particles on user call
  public async Task Generate() {

    SKColor bg = pColors[(int) Particle.bg];
    SKColor fill = pColors[currentParticle];

    for(int x = 0; x < Width; x++) {
      for(int y = 0; y < Height; y++) {

        if (random.Next(0, 100) < 5 && bitmap.GetPixel(x, y) == bg) {
          bitmap.SetPixel(x, y, fill);
        }

      }
    }

    await Update();
  }

  //Clears the screen upon user call
  public void Clear() {
    bitmap.Erase(pColors[(int) Particle.bg]);
  }

  //Clears the screen of a certain particle type upon user call
  public void ClearParticle(int type) {

    SKColor toClear = pColors[type];

    for(int x = 0; x < Width; x++) {
      for(int y = 0; y < Height; y++) {

        if (bitmap.GetPixel(x, y) == toClear) {
          bitmap.SetPixel(x, y, pColors[(int) Particle.bg]);
        }

      }
    }
  }

  //Generates the next bitmap state as particles undergo gravity
  private async Task NextBitmap() {

    if (isFrozen) {
      await Update();
      return;
    }

    SKColor bg = pColors[(int) Particle.bg];
    SKColor stone = pColors[(int) Particle.stone];
    SKColor sand = pColors[(int) Particle.sand];
    SKColor water = pColors[(int) Particle.water];

    int startY = (gravityDirection == 1) ? Height-2 : 1;
    bool hitFloor(int y) { return (gravityDirection == 1) ? y >= 0 : y <= Height-1; }
    
    for(int y = startY; hitFloor(y); y -= gravityDirection) {
      for(int x = 0; x < Width; x++) {

        //Handles non-moving stone and background particles
        SKColor here = bitmap.GetPixel(x, y);
        if (here == stone || here == bg) continue;

        //Handles all standard particle gravity
        SKColor below = bitmap.GetPixel(x, y+gravityDirection);
        if (below == bg) {
          bitmap.SetPixel(x, y, bg);
          bitmap.SetPixel(x, y+gravityDirection, here);
          continue;
        }

        //Handles left right movement for sand particles
        if (here == sand) {

          bool preferLeft = random.Next(0, 2) == 0;
          bool canLeft = 0 <= x-1 && x-1 < Width && bitmap.GetPixel(x-1, y+gravityDirection) == bg;
          bool canRight = 0 <= x+1 && x+1 < Width && bitmap.GetPixel(x+1, y+gravityDirection) == bg;


          if ((preferLeft || !canRight) && canLeft) {
            bitmap.SetPixel(x, y, bg);
            bitmap.SetPixel(x-1, y+gravityDirection, sand);
          } 
          else if (canRight) {
            bitmap.SetPixel(x, y, bg);
            bitmap.SetPixel(x+1, y+gravityDirection, sand);
          }
        }

        else if (here == water) {

          /* 
          // Doesn't work atm :(
          // Left side is zoomy and right side acts like sand (tho right could be fixed pretty easily prob)
          // Main issue seems like gravity moving on the left causes changes that screw with water spans
          int sX = x;
          while(x+1 < Width && bitmap.GetPixel(x+1, y) == water && bitmap.GetPixel(x+1, y+gravityDirection) != bg) {
            x++;
          }


          bool canLeft = false, continueLeft = true;
          int dxL = 0;

          while (!canLeft && continueLeft) {
            dxL++;

            if (continueLeft = 0 <= sX-dxL && sX-dxL < Width && bitmap.GetPixel(sX-dxL+1, y+gravityDirection) == water) {
              canLeft = bitmap.GetPixel(sX-dxL, y+gravityDirection) == bg;
            }
          }

          bool canRight = false, continueRight = true;
          int dxR = 0;

          while (!canRight && continueRight) {
            dxR++;

            if (continueRight = 0 <= x+dxR && x+dxR < Width && bitmap.GetPixel(x+dxR-1, y+gravityDirection) == water) {
              canRight = bitmap.GetPixel(x+dxR, y+gravityDirection) == bg;
            }
          }

          if (canLeft) {
            bitmap.SetPixel(sX, y, bg);
            bitmap.SetPixel(sX-dxL, y+gravityDirection, water);
          }
          if (canRight) {
            bitmap.SetPixel(x, y, bg);
            bitmap.SetPixel(x+dxR, y+gravityDirection, water);
          }
          */

          bool preferLeft = random.Next(0, 2) == 0;

          bool canLeft = false, canRight = false;
          bool continueLeft = true, continueRight = true;

          int dx = 0;

          while (!canLeft && !canRight && (continueLeft || continueRight)) {

            dx += 1;

            if (continueLeft = continueLeft && 0 <= x-dx-1 && x-dx-1 < Width && bitmap.GetPixel(x-dx, y+gravityDirection) == water) {
              canLeft = bitmap.GetPixel(x-dx-1, y+gravityDirection) == bg;
            }

            if (continueRight = continueRight && 0 <= x+dx+1 && x+dx+1 < Width && bitmap.GetPixel(x+dx, y+gravityDirection) == water) {
              canRight = bitmap.GetPixel(x+dx+1, y+gravityDirection) == bg;
            }
          }

          if ((preferLeft || !canRight) && canLeft) {
            bitmap.SetPixel(x, y, bg);
            bitmap.SetPixel(x-dx-1, y+gravityDirection, here);
          } 
          else if (canRight) {
            bitmap.SetPixel(x, y, bg);
            bitmap.SetPixel(x+dx+1, y+gravityDirection, here);
          }
        }
      }
    }
    await Update();
  }

  //Creates a circle at the given location using the current particle paint
  public void Click() {

    if (mouseArgsList.Count == 0) return;

    float startX = (float) Math.Round(mouseArgsList[0].OffsetX/Scale);
    float startY = (float) Math.Round(mouseArgsList[0].OffsetY/Scale);

    foreach (MouseEventArgs mouseArgs in mouseArgsList) {

      float x = (float) Math.Round(mouseArgs.OffsetX/Scale);
      float y = (float) Math.Round(mouseArgs.OffsetY/Scale);

      SKPaint paint = mouseArgs.ShiftKey ? pPaints[(int) Particle.bg] : pPaints[currentParticle];
      paint.StrokeWidth = 2*clickRadius;

      canvas.DrawLine(startX, startY, x, y, paint);
      
      startX = x; startY = y;
    }

    mouseArgsList.RemoveRange(0, mouseArgsList.Count - 1);
  }


  //Periodically runs next bitmap updates
  private async Task NextClock() {
    while (await nextTimer.WaitForNextTickAsync()) {
      await NextBitmap();
    }
  }

  //Checks periodically for clicks from the user
  private async Task ClickClock() {
    while (await clickTimer.WaitForNextTickAsync()) {
      if (isClicking) {
        Click();
      }
    }
  }


  //Properly disposes objects like timers to avoid issues on reload
  public void Dispose() {
    nextTimer.Dispose();
    nextTimer = null;
    clickTimer.Dispose();
    clickTimer = null;
    canvas.Dispose();
    canvas = null;
    bitmap.Dispose();
    bitmap = null;

    Console.WriteLine("Disposed");
  }
}