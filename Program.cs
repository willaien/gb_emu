// // See https://aka.ms/new-console-template for more information
// using System.Reflection;
// using System.Runtime.InteropServices;
// using SDL2;

// Console.WriteLine("Hello, World!");

// NativeLibrary.SetDllImportResolver(Assembly.GetAssembly(typeof(SDL)), DllImportResolver);

// SDL2.SDL.SDL_Init(SDL2.SDL.SDL_INIT_VIDEO);

// var window = SDL2.SDL.SDL_CreateWindow("Test", SDL.SDL_WINDOWPOS_UNDEFINED, SDL.SDL_WINDOWPOS_UNDEFINED, 640, 480, SDL.SDL_WindowFlags.SDL_WINDOW_SHOWN);

// var renderer = SDL.SDL_CreateRenderer(window, 
//                                         -1, 
//                                         SDL.SDL_RendererFlags.SDL_RENDERER_ACCELERATED | 
//                                         SDL.SDL_RendererFlags.SDL_RENDERER_PRESENTVSYNC);
// var running = true;
// // Main loop for the program
// while (running)
// {
//     // Check to see if there are any events and continue to do so until the queue is empty.
//     while (SDL.SDL_PollEvent(out SDL.SDL_Event e) == 1)
//     {
//         switch (e.type)
//         {
//             case SDL.SDL_EventType.SDL_QUIT:
//                 running = false;
//                 break;
//         }
//     }

// }

// // Clean up the resources that were created.
// SDL.SDL_DestroyRenderer(renderer);
// SDL.SDL_DestroyWindow(window);
// SDL.SDL_Quit();

// static IntPtr DllImportResolver(string libraryName, Assembly assembly, DllImportSearchPath? searchPath)
// {
//     if (libraryName == "SDL2.dll")
//     {
//         if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
//         {
//             var path = Path.Combine(Path.GetDirectoryName(assembly.Location), "libSDL2.dylib");
//             return NativeLibrary.Load(path);
//         }
//     }
//     return IntPtr.Zero;
// }

using gb_emu;

//Console.WriteLine("Beginning CPU Tests");

var cpu = new CPU();
cpu.Memory.LoadCartridge("09.gb");
while(true)
{
    cpu.NextCycle();
}

