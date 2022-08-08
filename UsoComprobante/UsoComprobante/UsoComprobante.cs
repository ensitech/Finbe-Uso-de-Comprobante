using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml.Serialization;
using UsoComprobante.AxService;

namespace UsoComprobante
{
    public class UsoComprobante : IPlugin
    {
        public void Execute(IServiceProvider serviceProvider)
        {
            IPluginExecutionContext context = (IPluginExecutionContext)serviceProvider.GetService(typeof(IPluginExecutionContext));

            if (context.InputParameters.Contains("Target") && context.InputParameters["Target"] is Entity)
            {
                IOrganizationServiceFactory serviceFactory = (IOrganizationServiceFactory)serviceProvider.GetService(typeof(IOrganizationServiceFactory));
                IOrganizationService service = serviceFactory.CreateOrganizationService(context.UserId);

                Entity entity = (Entity)context.InputParameters["Target"];
                Init(service, context.UserId, entity, context.MessageName);
            }
        }

        public void Init(IOrganizationService service, Guid userId, Entity entity, string messageName)
        {
            var configuration = GetConfigurations(service);
            var domainName = GetDomainName(service, userId);
            if (messageName == "Update")
            {
                entity = service.Retrieve(entity.LogicalName, entity.Id, new ColumnSet(new string[] {
                "fib_codigo",
                "fib_name",
                "fib_aplicapersonafisica",
                "fib_aplicapersonamoral"
                }));
            }

            SendRegimenFiscal(service, entity, configuration, domainName);

        }

        private void SendRegimenFiscal(IOrganizationService service, Entity entity, Entity configuration, string domainUserName)
        {
            string xml = SerializerToXml(new object[] {
                entity.Attributes["fib_codigo"],
                entity.Attributes["fib_name"],
                //entity.Attributes["fib_aplicapersonafisica"],
                //entity.Attributes["fib_aplicapersonamoral"],
                "2022-01-01",
                "4.0"
            });

            List<string> errors = new List<string>();

            AxServiceProd axServiceProd = new AxServiceProd();

            var connection = GetAxConnnections(configuration, 1, domainUserName);
            axServiceProd.Url = connection.Item2.ToString();
            var result = axServiceProd.createOrUpdateProposito(xml, connection.Item1);

            if (!result.Contains("Exito"))
                errors.Add("Compañia " + connection.Item1.empresa + " :" + result);

            connection = GetAxConnnections(configuration, 2, domainUserName);
            axServiceProd.Url = connection.Item2.ToString();
            result = axServiceProd.createOrUpdateProposito(xml, connection.Item1);

            if (!result.Contains("Exito"))
                errors.Add("Compañia " + connection.Item1.empresa + " :" + result);

            connection = GetAxConnnections(configuration, 3, domainUserName);
            axServiceProd.Url = connection.Item2.ToString();
            result = axServiceProd.createOrUpdateProposito(xml, connection.Item1);

            if (!result.Contains("Exito"))
                errors.Add("Compañia " + connection.Item1.empresa + " :" + result);

            if (errors.Any())
                throw new Exception(string.Join("|", errors));
        }

        private string SerializerToXml(object[] parameters)
        {
            try
            {
                StringWriter stringWriter = new StringWriter();
                new XmlSerializer(parameters.GetType()).Serialize((TextWriter)stringWriter, (object)parameters);
                string str = stringWriter.ToString();
                stringWriter.Close();
                return str;
            }
            catch (Exception ex)
            {
                throw new InvalidPluginExecutionException("Error: " + ex.Message);
            }
        }

        private Tuple<InfoConexion, Uri> GetAxConnnections(Entity configuration, int clientType, string domainUserName)
        {
            if (clientType == 1)
            {
                return new Tuple<InfoConexion, Uri>(
                    new InfoConexion
                    {
                        empresa = configuration.Attributes["fib_fil_companiaax"].ToString(),
                        servidor = configuration.Attributes["fib_fil_servidorax"].ToString(),
                        dominio = domainUserName.Split('\\')[0],
                        usuario = domainUserName.Split('\\')[1]
                    },
                    new Uri(configuration.Attributes["fib_fil_urlwebserviceax"].ToString()));

            }
            else if (clientType == 2)
            {
                return new Tuple<InfoConexion, Uri>(
                    new InfoConexion
                    {
                        empresa = configuration.Attributes["fib_ter_companiaax"].ToString(),
                        servidor = configuration.Attributes["fib_ter_servidorax"].ToString(),
                        dominio = domainUserName.Split('\\')[0],
                        usuario = domainUserName.Split('\\')[1]
                    },
                    new Uri(configuration.Attributes["fib_ter_urlwebserviceax"].ToString()));

            }
            else
            {
                return new Tuple<InfoConexion, Uri>(
                    new InfoConexion
                    {
                        empresa = configuration.Attributes["fib_arr_companiaax"].ToString(),
                        servidor = configuration.Attributes["fib_arr_servidorax"].ToString(),
                        dominio = domainUserName.Split('\\')[0],
                        usuario = domainUserName.Split('\\')[1]
                    },
                    new Uri(configuration.Attributes["fib_arr_urlwebserviceax"].ToString()));

            }
        }

        private Entity GetConfigurations(IOrganizationService service)
        {
            string fetch = "<fetch version='1.0' output-format='xml-platform' mapping='logical' distinct='false'>       " +
                        "<entity name='fib_configuracion'>          " +
                        "<attribute name='fib_fil_usuarioax'/>          " +
                        "<attribute name='fib_fil_dominiousrax'/>          " +
                        "<attribute name='fib_fil_companiaax'/>          " +
                        "<attribute name='fib_fil_servidorax'/>          " +
                        "<attribute name='fib_fil_urlwebserviceax'/>      " +
                        "<attribute name='fib_ter_usuarioax'/>          " +
                        "<attribute name='fib_ter_dominiousrax'/>          " +
                        "<attribute name='fib_ter_companiaax'/>          " +
                        "<attribute name='fib_ter_servidorax'/>          " +
                        "<attribute name='fib_ter_urlwebserviceax'/>      " +
                        "<attribute name='fib_arr_usuarioax'/>          " +
                        "<attribute name='fib_arr_dominiousrax'/>          " +
                        "<attribute name='fib_arr_companiaax'/>          " +
                        "<attribute name='fib_arr_servidorax'/>          " +
                        "<attribute name='fib_arr_urlwebserviceax'/>      " +
                        "</entity>  " +
                        "</fetch>";
            var retrive = service.RetrieveMultiple(new FetchExpression(fetch));

            return retrive.Entities.FirstOrDefault();
        }

        private string GetDomainName(IOrganizationService service, Guid userId)
        {
            var user = service.Retrieve("systemuser", userId, new ColumnSet("domainname"));
            return user.Attributes["domainname"].ToString();
        }
    }
}
