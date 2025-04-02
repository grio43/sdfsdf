extern alias SC;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using SC::SharedComponents.Py;

namespace EVESharpCore.Controllers
{
    public class CallableWrapperHandler
    {
        private static readonly Lazy<CallableWrapperHandler> _instance =
            new Lazy<CallableWrapperHandler>(() => new CallableWrapperHandler());

        private static ConcurrentBag<CallableWrapperEntity> _ents;

        private CallableWrapperHandler()
        {
            _ents = new ConcurrentBag<CallableWrapperEntity>();
        }

        public static CallableWrapperHandler Instance => _instance.Value;

        public IntPtr CreateCallableWrapperEntity(PyObject callable)
        {
            var wrapperEntity = new CallableWrapperEntity();
            var pyObj = wrapperEntity.SetCallable(callable);
            _ents.Add(wrapperEntity);
            return pyObj;
        }
    }

    public class CallableWrapperEntity : IDisposable
    {
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate IntPtr PyDelegate(IntPtr self, IntPtr args, IntPtr kw);
        
        private PyDelegate _pyDelegate;
        private IntPtr _pyDelegateCallableObjPtr;
        private IntPtr _callableOriginal;
        private Py.PyMethodDef _md;
        private IntPtr _mdAllocPtr;
        public CallableWrapperEntity()
        {

        }

        public IntPtr SetCallable(PyObject callable)
        {
            _callableOriginal = callable.PyRefPtr;
            _pyDelegate = new PyDelegate(Callback);
            _pyDelegateCallableObjPtr = CreateDelegatePyObj(_pyDelegate);
            //var pyObj = new PyObject(_pySharp, _pyDelegateCallableObjPtr, true);

            return _pyDelegateCallableObjPtr;
        }

        public IntPtr Callback(IntPtr self, IntPtr args, IntPtr kw)
        {
            //var argPy = new PyObject(_pySharp, args, true);
            //var kwPy = new PyObject(_pySharp, kw, true);
            //Log($"ARGS {argPy.LogObject()}");
            //Log($"KW {kwPy.LogObject()}");
            //var res = Py.PyEval_CallObjectWithKeywords(_callableOriginal, args, kw);
            Thread.Sleep(1000);
            //Py.PyObject_Call(_callableOriginal, args, kw);
            //Py.PyEval_CallObjectWithKeywords(_callableOriginal, args, kw);

            Log("Called!");
            //return PySharp.PyNone;
            return IntPtr.Zero;
        }

        public void Log(string message)
        {
            Logging.Log.WriteLine(message);
        }

        public IntPtr CreateDelegatePyObj(Delegate value)
        {
            _md.ml_doc = IntPtr.Zero;
            _md.ml_name = Marshal.StringToHGlobalAnsi("");
            _md.ml_meth = Marshal.GetFunctionPointerForDelegate(value);
            _md.ml_flags = 0x0001 | 0x0002; // METH_VARARGS | METH_KEYWORDS
            var name = Py.PyString_FromString("");
            _mdAllocPtr = Marshal.AllocHGlobal(Marshal.SizeOf(_md));
            Marshal.StructureToPtr(_md, _mdAllocPtr, true);
            var result = Py.PyCFunction_NewEx(_mdAllocPtr, IntPtr.Zero, name);
            return result;
        }

        public void Dispose()
        {

        }
    }

}
