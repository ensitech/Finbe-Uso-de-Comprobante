using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Xrm.Sdk;

namespace UnitTestUsoComprobante
{
    [TestClass]
    public class UnitTest1
    {
        [TestMethod]
        public void TestMethod1()
        {
            var plugin = new UsoComprobante.UsoComprobante();
            var entity = new Entity();
            entity.Id = new Guid("176C2DA8-3ADC-EC11-BD3F-0050569D79D0");
            entity.LogicalName = "fib_usocomprobante";
            plugin.Init(CRMLogin.createService(), new Guid("B437A414-0B33-EA11-8B50-0050569D79D0"), entity, "Update");
        }
    }
}
