using System;
using System.Windows.Forms;
using System.Collections.Generic;

public static class ControlExtensions
{
    /// <summary>
    /// Executes the Action on the UI thread.  Blocks calling thread.
    /// </summary>
    public static void UIThread(this Control @this, Action code)
    {
        if (@this.InvokeRequired)
        {
            if (@this.IsDisposed || @this.Disposing)
                return;
            @this.Invoke(code);
            //IAsyncResult result = @this.BeginInvoke(code);
            //while (!result.IsCompleted)
            //System.Threading.Thread.Sleep(1);
        }
        else
        {
            code.Invoke();
        }
    }
}