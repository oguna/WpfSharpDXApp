using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SharpDX;

namespace WpfSharpDXApp
{
    class Camera
    {
        private float r;
        private float theta;
        private float phi;
        private Vector3 eye;
        private Vector3 at;
        private Vector3 up;

        public Matrix View;

        public Camera()
        {
            Reset();
        }

        public void Reset()
        {
            View = Matrix.Identity;
            eye = new Vector3(0, 0, -0.3f);
            at = new Vector3(0, 0, 1);
            up = new Vector3(0, 1, 0);
        }

        public void Update()
        {
            View = Matrix.LookAtLH(eye + at, at, up);
        }

        public void UpdatePosition()
        {
            eye = new Vector3(r * (float)Math.Sin(theta) * (float)Math.Cos(phi), r * (float)Math.Sin(phi), -r * (float)Math.Cos(theta) * (float)Math.Cos(phi));
        }

        public float Radius
        {
            get { return r; }
            set
            {
                this.r = value;
                UpdatePosition();
            }
        }

        public float Theta
        {
            get { return theta; }
            set
            {
                this.theta = value;
                UpdatePosition();
            }
        }

        public float Phi
        {
            get { return phi; }
            set
            {
                this.phi = value;
                UpdatePosition();
            }
        }

        public Vector3 Up
        {
            get { return up; }
        }

        public Vector3 Eye
        {
            get { return eye; }
        }

        public float CenterDepth
        {
            set
            {
                at = new Vector3(0, 0, value);
            }
        }
    }
}
