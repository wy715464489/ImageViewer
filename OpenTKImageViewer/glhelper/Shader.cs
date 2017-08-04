﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OpenTK.Graphics.OpenGL4;

namespace OpenTKImageViewer.glhelper
{
    public class Shader
    {
        public int Id { get; private set; }
        public string Source { get; set; }
        private ShaderType type;

        public Shader(ShaderType type)
        {
            this.type = type;
            Id = GL.CreateShader(type);
        }

        public void Compile()
        {
            GL.ShaderSource(Id, Source);
            GL.CompileShader(Id);

            int status;
            GL.GetShader(Id, ShaderParameter.CompileStatus, out status);
            
            if (status == 0)
                throw new Exception($"Error Compiling {type.ToString()} Shader: {GL.GetShaderInfoLog(Id)}");
        }
    }
}