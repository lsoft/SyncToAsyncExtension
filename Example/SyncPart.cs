using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Zomp.SyncMethodGenerator;

namespace SyncAsyncExample
{
    internal partial class MyClass
    {
        public void Method0()
        {
            throw new NotImplementedException();
        }

        public async Task Method0Async()
        {
            throw new NotImplementedException();
        }




        public void Method1()
        {
            throw new NotImplementedException();
        }

        public async Task Method1Async(IProgress<object> p, CancellationToken ct)
        {
            throw new NotImplementedException();
        }



        public object Method2()
        {
            throw new NotImplementedException();
        }

        public async Task<object> Method2Async()
        {
            throw new NotImplementedException();
        }



        public T Method3<T>()
        {
            throw new NotImplementedException();
        }

        public async Task<T> Method3Async<T>()
        {
            throw new NotImplementedException();
        }




        public async Task<T> Method4Async<T>()
        {
            throw new NotImplementedException();
        }




        public T Method5<T>()
        {
            throw new NotImplementedException();
        }

        public Task<T> Method5Async<T>()
        {
            throw new NotImplementedException();
        }




        /// <summary>
        /// ISG test.
        /// </summary>
        [CreateSyncVersion]
        public async Task<T> Method6Async<T>()
        {
            throw new NotImplementedException();
        }





        public T Method7<T>(string a)
        {
            throw new NotImplementedException();
        }

        public Task<T> Method7Async<T>(System.String b)
        {
            throw new NotImplementedException();
        }



        public System.String Method8(string a)
        {
            throw new NotImplementedException();
        }

        public Task<string> Method8Async(System.String b)
        {
            throw new NotImplementedException();
        }



        public System.String Method9<T>(T a)
        {
            throw new NotImplementedException();
        }

        public Task<string> Method9Async<T>(T b)
        {
            throw new NotImplementedException();
        }




    }
}
