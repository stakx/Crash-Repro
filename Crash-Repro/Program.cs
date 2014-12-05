using System;
using System.Linq.Expressions;
using Castle.DynamicProxy;
using StructureMap;

namespace Crash_Repro {
    public class Program {
        private static void Main(string [] args) {
            // Initialize the StructureMap IoC container with DynamicProxy based interception for logging
            Bootstrapper.Initialize();

            // Initialize the view and get something done
            IView view = new MyView();
            view.PrintNameAndAge();

            Console.ReadKey(true);
        }
    }

    #region Dummy View

    public interface IView {
        bool NeedFemaleName { get; }
        void PrintNameAndAge();
    }

    public class MyView : IView {
        private readonly ILogic _logic;

        public MyView() {
            _logic = Bootstrapper.GetLogic<IView, ILogic>(this);
        }

        public bool NeedFemaleName { get { return false; } }

        public void PrintNameAndAge() {
            Console.WriteLine("Name: {0} | Age: {1}", _logic.GetName(), _logic.GetAge());
        }
    }

    #endregion

    #region Logic - depends on Service

    public interface ILogic {
        string GetName();
        int GetAge();
    }

    public class MyLogic : ILogic {
        private readonly IService _service;
        private readonly IView _view;

        public MyLogic(IView view, IService service) {
            _view = view;
            _service = service;
        }

        public string GetName() {
            return _view.NeedFemaleName ? _service.GetFemaleName() : _service.GetMaleName();
        }

        public int GetAge() {
            return _service.GetAge();
        }
    }

    #endregion

    #region Service

    public interface IService {
        string GetMaleName();
        string GetFemaleName();
        int GetAge();
    }

    public class MyService : IService {
        public string GetMaleName() {
            return "John Doe";
        }

        public string GetFemaleName() {
            return "Jane Doe";
        }

        public int GetAge() {
            return 42;
        }
    }

    #endregion

    #region Dynamic Proxy

    public class DynamicProxy {
        public static Expression<Func<T, T>> MyInterceptorFor<T>() {
            return s => CreateInterface(typeof (T), s);
        }

        private static T CreateInterface<T>(Type interfaceType, T concreteObject) {
            return (T) new ProxyGenerator().CreateInterfaceProxyWithTargetInterface(interfaceType, concreteObject, new MyInterceptor());
        }
    }

    public class MyInterceptor : IInterceptor {
        public void Intercept(IInvocation invocation) {
            Console.WriteLine("Called {0}::{1}\n", invocation.TargetType.Name, invocation.Method.Name);
            invocation.Proceed();
        }
    }

    #endregion

    #region StructureMap Boostrapper

    public static class Bootstrapper {
        private static readonly Container _container = new Container();

        public static void Initialize() {
            _container.Configure(c => {
                                     c.For<ILogic>().DecorateAllWith(DynamicProxy.MyInterceptorFor<ILogic>()).Use<MyLogic>();
                                     c.For<IService>().DecorateAllWith(DynamicProxy.MyInterceptorFor<IService>()).Use<MyService>();
                                 });
        }

        public static TLogic GetLogic<TView, TLogic>(TView view) {
            return _container.With(view).GetInstance<TLogic>();
        }
    }

    #endregion
}
