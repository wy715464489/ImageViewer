﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Microsoft.Win32;
using SharpGL;
using SharpGL.SceneGraph;
using SharpGL.SceneGraph.Shaders;
using TextureViewer.glhelper;
using TextureViewer.ImageView;

namespace TextureViewer
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private readonly App parent;
        public ImageLoaderWrapper.Image Image { get; private set; }
        public ulong ZIndex { get; set; }

        private String errorMessage = "";

        private IImageView currentView;

        // mouse tracking
        private Point mousePosition = new Point();

        public MainWindow(App parent, ImageLoaderWrapper.Image file)
        {
            this.parent = parent;
            this.Image = file;
            this.ZIndex = 0;

            InitializeComponent();
            
            if (file == null)
                currentView = new EmptyView();
            else
                currentView = new SingleView();

            this.Title = getWindowName(file);
        }

        public ListBoxItem[] GenerateMipMapItems()
        {
            if(Image == null)
                return new ListBoxItem[0];

            var items = new ListBoxItem[Image.GetNumMipmaps()];
            // generate mip map previews
            for (int curMipmap = 0; curMipmap < Image.GetNumMipmaps(); ++curMipmap)
            {
                items[curMipmap] = new ListBoxItem {Content = Image.GetWidth(curMipmap).ToString() + "x" + Image.GetHeight(curMipmap).ToString()};
            }
            return items;
        }

        public ListBoxItem[] GenerateLayerItems()
        {
            if(Image == null)
                return new ListBoxItem[0];
            var items = new ListBoxItem[Image.Layers.Count];
            for (int curLayer = 0; curLayer < Image.Layers.Count; ++curLayer)
            {
                items[curLayer] = new ListBoxItem{Content = "Layer " + curLayer};
            }
            return items;
        }

        private void OpenGLControl_OnOpenGLDraw(object sender, OpenGLEventArgs args)
        {
            if (errorMessage.Length > 0)
            {
                MessageBox.Show(errorMessage);
                errorMessage = "";
            }
            try
            {
                //  Get the OpenGL instance that's been passed to us.
                OpenGL gl = args.OpenGL;
                Utility.GlCheck(gl);

                gl.MatrixMode(OpenGL.GL_PROJECTION);
                gl.LoadIdentity();

                gl.MatrixMode(OpenGL.GL_MODELVIEW);
                gl.LoadIdentity();

                //  Clear the color and depth buffers.
                gl.ClearColor(0.9333f, 0.9333f, 0.9333f, 1.0f);
                gl.Clear(OpenGL.GL_COLOR_BUFFER_BIT | OpenGL.GL_DEPTH_BUFFER_BIT);
                Utility.GlCheck(gl);

                currentView.Draw();
                Utility.GlCheck(gl);

                //  Flush OpenGL.
                gl.Flush();
            }
            catch (Exception e)
            {
                errorMessage = e.Message + "\nstack: " + e.StackTrace;
            }
           
        }

        private void OpenGLControl_OnOpenGLInitialized(object sender, OpenGLEventArgs args)
        {
            try
            {
                //args.OpenGL.PixelStore(OpenGL.GL_PACK_ALIGNMENT, 1);
                currentView.Init(args.OpenGL, this);

                Utility.GlCheck(args.OpenGL);
            }
            catch (Exception e)
            {
                errorMessage = e.Message + "\nstack: " + e.StackTrace;
            }
        }

        private void MenuItem_Click_Mipmaps(object sender, RoutedEventArgs e)
        {
            parent.OpenDialog(App.UniqueDialog.Mipmaps);
        }

        private void MenuItem_Click_Layers(object sender, RoutedEventArgs e)
        {
            parent.OpenDialog(App.UniqueDialog.Layer);
        }

        private void MainWindow_OnClosing(object sender, CancelEventArgs e)
        {
            parent.UnregisterWindow(this);
        }

        private string getWindowName(ImageLoaderWrapper.Image image)
        {
            if (image == null)
                return "Texture Viewer";
            return "Texture Viewer - " + image.Filename;
        }

#region OpenGL Control Mouse Interaction

        private bool mouseDown = false;

        private void OpenGlControl_OnMouseMove(object sender, MouseEventArgs e)
        {
            var newPosition = e.GetPosition(this.OpenGlControl);
            if (mouseDown)
            {
                // drag event
                var diff = newPosition - mousePosition;
                
                if(Math.Abs(diff.X) > 0.01 || Math.Abs(diff.Y) > 0.01)
                    currentView.OnDrag(diff);
            }
            mousePosition = newPosition;
        }

        private void OpenGlControl_OnMouseDown(object sender, MouseButtonEventArgs e)
        {
            mouseDown = e.LeftButton == MouseButtonState.Pressed || e.RightButton == MouseButtonState.Pressed;
            mousePosition = e.GetPosition(this.OpenGlControl);
        }

        private void OpenGlControl_OnMouseUp(object sender, MouseButtonEventArgs e)
        {
            mouseDown = e.LeftButton == MouseButtonState.Pressed || e.RightButton == MouseButtonState.Pressed;
            mousePosition = e.GetPosition(this.OpenGlControl);
        }

        private void OpenGlControl_OnMouseLeave(object sender, MouseEventArgs e)
        {
            mouseDown = false;
        }

        private void OpenGlControl_OnMouseWheel(object sender, MouseWheelEventArgs e)
        {
            currentView.OnScroll((double)e.Delta, e.GetPosition(OpenGlControl));
        }

#endregion

        public int GetClientWidth()
        {
            return (int)OpenGlControl.ActualWidth;
        }

        public int GetClientHeight()
        {
            return (int)OpenGlControl.ActualHeight;
        }

        private void LinearFiltering_OnChecked(object sender, RoutedEventArgs routedEventArgs)
        {
            currentView?.SetImageFilter(MenuItemLinearInterpolation.IsChecked
                ? OpenGL.GL_LINEAR
                : OpenGL.GL_NEAREST);
        }

        private void OpenFile_OnClick(object sender, RoutedEventArgs e)
        {
            OpenFileDialog ofd = new OpenFileDialog();
            ofd.Multiselect = false;
            
            if (ofd.ShowDialog(this) == true)
                parent.SpawnWindow(ofd.FileName);
        }
        
        private void OpenGlControl_OnDrop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
                
                // TODO add file to current window if definitions match
                if (files != null)
                    foreach (var file in files)
                        parent.SpawnWindow(file);
            }

        }

        private void MainWindow_OnActivated(object sender, EventArgs e)
        {
            parent.SetActiveWindow(this);
        }

        private void MainWindow_OnDeactivated(object sender, EventArgs e)
        {
            parent.UpdateDialogVisibility();
        }

        public ListBoxItem[] GenerateImageItems()
        {
            return new ListBoxItem[0];
        }

        private void MenuItem_Click_Images(object sender, RoutedEventArgs e)
        {
            parent.OpenDialog(App.UniqueDialog.Image);
        }


    }
}
