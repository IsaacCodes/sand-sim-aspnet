using Microsoft.AspNetCore.Components;
using System.Drawing;

namespace BlazorApp.Components.Shared;
public class PixelMapBase : ComponentBase {

  [Parameter]
  public int Width { get; set; }
  [Parameter]
  public int Height { get; set; }

  private void Update() {
    /*
    bitmap.Save("wwwroot/images/output.png");
    StateHasChanged();
    */
  }

  protected override void OnInitialized() {
    Console.WriteLine("Init");

    
    //Bitmap bitmap = new Bitmap(Width, Height);
    //bitmap.Save("Components/Shared/output.png");
    //StateHasChanged();
  }

  public void Generate() {

    /*
    Bitmap bitmap = new Bitmap(Width, Height);


    for(int x = 0; x < Width; x++) {
      for(int y = 0; y < Height; y++) {

        if (x > y) {
          bitmap.SetPixel(x, y, Color.Red);
        }

      }
    }

    Console.WriteLine("yoooooooo");

    Update(bitmap);
    */
    Console.WriteLine("yoooooooo");
  }
}