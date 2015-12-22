using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WpfSharpDXApp
{
    interface ID3D11App
    {
        void InitDevice();

        void Render(IntPtr resource, bool isNewSurface);
    }
}
