﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Forms.Integration;
using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL4;
using TextureViewer.Annotations;

namespace TextureViewer.Models
{
    public class OpenGlContext : INotifyPropertyChanged
    {
        public struct Size
        {
            public int Width { get; set; }
            public int Height { get; set; }
        }

        private bool debugGl = true;

        public GLControl GlControl { get; }

        public OpenGlContext(MainWindow window)
        {
            try
            {
                var flags = GraphicsContextFlags.Default;
                if (debugGl)
                    flags |= GraphicsContextFlags.Debug;

                // init opengl for version 4.2
                GlControl = new GLControl(new GraphicsMode(new ColorFormat(32), 32), 4, 2, flags)
                {
                    Dock = DockStyle.Fill
                };

                window.OpenGlHost.Child = GlControl;

                // initialize client size
                window.Loaded += (sender, args) => 
                {
                    var source = PresentationSource.FromVisual(window);
                    var scalingX = source.CompositionTarget.TransformToDevice.M11;
                    var scalingY = source.CompositionTarget.TransformToDevice.M22;
                    // update client size
                    ClientSize = new Size()
                    {
                        Width = (int)(window.OpenGlHost.ActualWidth * scalingX),
                        Height = (int)(window.OpenGlHost.ActualHeight * scalingY)
                    };

                    // change size with callback
                    window.OpenGlHost.SizeChanged += (sender2, args2) =>
                    {
                        ClientSize = new Size()
                        {
                            Width = (int)(window.OpenGlHost.ActualWidth * scalingX),
                            Height = (int)(window.OpenGlHost.ActualHeight * scalingY)
                        };
                    };
                };

                GlControl.DragOver += (o, args) => args.Effect = System.Windows.Forms.DragDropEffects.Copy;
                GlControl.AllowDrop = true;

                Enable();

                GL.Enable(EnableCap.TextureCubeMapSeamless);
                GL.PixelStore(PixelStoreParameter.PackAlignment, 1);
                GL.PixelStore(PixelStoreParameter.UnpackAlignment, 1);
            }
            catch (Exception e)
            {
                Console.Write(e);
                throw;
            }
            finally
            {
                Disable();
            }
        }

        private Size clientSize = new Size(){Height = 0, Width = 0}; 
        public Size ClientSize
        {
            get => clientSize;
            private set
            {
                if (value.Width == clientSize.Width && value.Height == clientSize.Height) return;
                clientSize = value;
                OnPropertyChanged(nameof(ClientSize));
            }
        }

        public bool IsEnabled { get; private set; } = false;

        /// <summary>
        /// makes the window opengl context current
        /// </summary>
        public void Enable()
        {
            GlControl?.MakeCurrent();
            if (debugGl)
                glhelper.Utility.EnableDebugCallback();
            IsEnabled = true;
        }

        /// <summary>
        /// flushes commands and makes a null opengl context current
        /// </summary>
        public void Disable()
        {
            GL.Flush();
            if (debugGl)
                GL.Disable(EnableCap.DebugOutput);

            try
            {
                GlControl?.Context.MakeCurrent(null);
            }
            catch (GraphicsContextException)
            {
                // happens sometimes..
            }

            IsEnabled = false;
        }

        /// <summary>
        /// the frame will be redrawn as soon as possible
        /// </summary>
        public void RedrawFrame()
        {
            GlControl?.Invalidate();
        }

        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}