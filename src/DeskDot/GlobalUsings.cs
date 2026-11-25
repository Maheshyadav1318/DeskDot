// Explicit, minimal global usings.
//
// Implicit usings stay DISABLED on purpose: with UseWindowsForms enabled the SDK would
// import System.Windows.Forms and System.Drawing globally, which makes WPF types such as
// Application, KeyEventArgs, Color and Point ambiguous across the project.

global using System;
global using System.Collections.Generic;
global using System.IO;
global using System.Linq;
global using System.Threading;
global using System.Threading.Tasks;
global using System.Windows.Threading;
