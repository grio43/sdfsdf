extern alias SC;

using SC::SharedComponents.Py;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static EVESharpCore.Framework.DirectStaticDataLoader;

namespace EVESharpCore.Framework
{

    public class DirectStaticDataLoader
    {
        public class DynamicItemAttributeMutator
        {
            public Dictionary<int, DynamicItemAttributeMutatorAttribute> Attributes;

            public DynamicItemAttributeMutator(PyObject pyRef)
            {
                Attributes = new Dictionary<int, DynamicItemAttributeMutatorAttribute>();
                foreach(var item in pyRef["attributeIDs"].ToDictionary<int>())
                {
                    Attributes.Add(item.Key, new DynamicItemAttributeMutatorAttribute(item.Value));
                }
            }

            public class DynamicItemAttributeMutatorAttribute
            {

                public DynamicItemAttributeMutatorAttribute(PyObject pyRef)
                {
                    this.Min = pyRef["min"].ToFloat();
                    this.Max = pyRef["max"].ToFloat();
                }

                public float Min { get; private set; }
                public float Max { get; private set; }
            }
        }

        private DirectEve de;

        private Dictionary<int, DynamicItemAttributeMutator> _dynamicItemAttributeMutators;
        public Dictionary<int, DynamicItemAttributeMutator> DynamicItemAttributeMutators
        {
            get
            {
                if (_dynamicItemAttributeMutators != null)
                    return _dynamicItemAttributeMutators;


                _dynamicItemAttributeMutators = new Dictionary<int, DynamicItemAttributeMutator>();
                var data = LoadData("dynamicitemattributes").ToDictionary<int>();

                foreach(var item in data)
                {
                    _dynamicItemAttributeMutators.Add(item.Key, new DynamicItemAttributeMutator(item.Value));
                }


                return _dynamicItemAttributeMutators;
            }
        }

        public DirectStaticDataLoader(DirectEve de)
        {
            this.de = de;
        }

        private PyObject LoadData(String cls)
        {

            //var da = de.PySharp.Import("dynamicitemattributes");
            var da = de.PySharp.Import(cls);

            if (da.IsValid)
            {
                var res = da.Call("GetData");
                return res;
            }

            return null;
        }
    }
}
