using Sandbox.Game.EntityComponents;
using Sandbox.ModAPI.Ingame;
using Sandbox.ModAPI.Interfaces;
using SpaceEngineers.Game.ModAPI.Ingame;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using System.Text;
using System;
using VRage.Collections;
using VRage.Game.Components;
using VRage.Game.GUI.TextPanel;
using VRage.Game.ModAPI.Ingame.Utilities;
using VRage.Game.ModAPI.Ingame;
using VRage.Game.ObjectBuilders.Definitions;
using VRage.Game;
using VRage;
using VRageMath;

namespace IngameScript
{
    partial class Program : MyGridProgram
    {
        IMyTextSurface _drawingSurface;
        RectangleF _viewport;

        // Script constructor
        public Program()
        {
            // Me is the programmable block which is running this script.
            // Retrieve the Large Display, which is the first surface
            _drawingSurface = Me.GetSurface(0);

            StringBuilder sprts = new StringBuilder();
            List<String> sprites = new List<String>();
            _drawingSurface.GetSprites(sprites);

            sprites.ForEach(sprt =>
            {
                sprts.Append($"{sprt}\n");
            });
            Me.CustomData = "";
            Me.CustomData = sprts.ToString();

            // Set the continuous update frequency of this script
            //Runtime.UpdateFrequency = UpdateFrequency.Update100;

            IMyTextPanel lcd = (IMyTextPanel)GridTerminalSystem.GetBlockWithName("Shitty Fucky Test LCD");
            _drawingSurface = lcd;

            // Calculate the viewport by centering the surface size onto the texture size
            _viewport = new RectangleF(
                (_drawingSurface.TextureSize - _drawingSurface.SurfaceSize) / 2f,
                _drawingSurface.SurfaceSize
            );

            // Make the text surface display sprites
            PrepareTextSurfaceForSprites(_drawingSurface);
        }

        // Main Entry Point
        public void Main(string argument, UpdateType updateType)
        {
            int percentage;
            Echo("");
            if (!Int32.TryParse(argument, out percentage))
            {
                percentage = 50;
                Echo($"Invalid value \"{argument}\"");
            }

            List<IMyJumpDrive> drives = new List<IMyJumpDrive>();
            GridTerminalSystem.GetBlocksOfType(drives, drive => drive.CubeGrid == Me.CubeGrid && drive.IsWorking);

            float maxCharge = 0;
            float currentCharge = 0;

            drives.ForEach(drive => {
                maxCharge += drive.MaxStoredPower;
                currentCharge += drive.CurrentStoredPower;
            });

            int chargePercentage = (int) (currentCharge/maxCharge)*100;

            PercentageBar(
                txtSurface: _drawingSurface,
                height: 15,
                value: chargePercentage,
                topPadding: 10,
                rightPadding: 0,
                leftPadding: 0
            );
            /*// Begin a new frame
            var frame = _drawingSurface.DrawFrame();

            // All sprites must be added to the frame here
            DrawSprites(ref frame);

            // We are done with the frame, send all the sprites to the text panel
            frame.Dispose();*/
        }

        public void PercentageBar(IMyTextSurface txtSurface, int height, int value, int topPadding = 10, int rightPadding = 0, int leftPadding = 0)
        {
            RectangleF viewport = new RectangleF((txtSurface.TextureSize - txtSurface.SurfaceSize) / 2f, txtSurface.SurfaceSize);
            var frame = txtSurface.DrawFrame();
            txtSurface.ScriptBackgroundColor = Color.Black;

            float heightInPixel = (viewport.Height) / ((float) 100 / (height / 2));
            float leftPadPixel  = (viewport.Width) / ((float) 100 / leftPadding);
            float rightPadPixel = (viewport.Width) / ((float) 100 / rightPadding);
            float topPadPixel   = (viewport.Height) / ((float)100 / topPadding);
            float valuePixel    = (viewport.Width - (rightPadPixel + leftPadPixel)) / ((float)100 / value);
            

            Vector2 startPos    = new Vector2(leftPadPixel, topPadPixel);
            Vector2 size        = new Vector2(valuePixel, heightInPixel);
            Vector2 borderSize  = new Vector2((float) viewport.Width - (rightPadPixel + leftPadPixel), heightInPixel + 20);
            

            var sprite = new MySprite()
            {
                Type = SpriteType.TEXTURE,
                Data = "SquareSimple",
                Color = Color.Green,
                Size = size,
                Position = startPos
            };
            

            var border = new MySprite()
            {
                Type = SpriteType.TEXTURE,
                Data = "SquareHollow",
                Color = Color.Red,
                Size = borderSize,
                Position = startPos
            };
            frame.Add(sprite);
            frame.Add(border);

            frame.Dispose();
        }

        // Drawing Sprites
        public void DrawSprites(ref MySpriteDrawFrame frame)
        {                   

            Vector2 size = new Vector2(_viewport.Width/2, _viewport.Height / 10);
            Vector2 pos = new Vector2(size.X / 2 + 10, size.Y / 2 + 10 );

            // Create background sprite
            var sprite = new MySprite()
            {
                Type = SpriteType.TEXTURE,
                Data = "SquareSimple",
                //Position = _viewport.Center,
                //Size = _viewport.Size,
                Position = pos,
                Size = size,
                Color = _drawingSurface.ScriptForegroundColor.Alpha(0.66f),
                Alignment = TextAlignment.CENTER
            };
            // Add the sprite to the frame
            frame.Add(sprite);

            // Set up the initial position - and remember to add our viewport offset
            var position = new Vector2(256, 20) + _viewport.Position;

            // Create our first line
            sprite = new MySprite()
            {
                Type = SpriteType.TEXT,
                Data = "Line 1",
                Position = position,
                RotationOrScale = 0.8f /* 80 % of the font's default size */,
                Color = Color.Red,
                Alignment = TextAlignment.CENTER /* Center the text on the position */,
                FontId = "White"
            };
            // Add the sprite to the frame
            frame.Add(sprite);
        }

        // Auto-setup text surface
        public void PrepareTextSurfaceForSprites(IMyTextSurface textSurface)
        {
            // Set the sprite display mode
            textSurface.ContentType = ContentType.SCRIPT;
            // Make sure no built-in script has been selected
            textSurface.Script = "";
        }
    }
}
