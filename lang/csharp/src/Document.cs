using Antlr4.Runtime;
using Antlr4.Runtime.Tree;
using STEP;
using System;
using System.Collections;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Collections.Generic;
using System.Reflection;

namespace IFC
{
    /// <summary>
    /// A Document contains a collection of entities.
    /// </summary>
    public class Document
    {
        private IDictionary<Guid, BaseIfc>  storage = new Dictionary<Guid, BaseIfc>();

        private const string APPNAME = "IFC-dotnet";

        /// <summary>
        /// Get all entities in the document.
        /// </summary>
        public IEnumerable<BaseIfc> AllEntities
        {
            get{return storage.Values;}
        }

        /// <summary>
        /// Construct a document.
        /// 
        /// The document will have all unit types set to SI units.
        /// </summary>
        /// <param name="projectName">The name of the project.</param>
        /// <param name="projectDescription">The description of the project.</param>
        /// <param name="userId">The id of the user.</param>
        /// <param name="userLastName">The last name of the user.</param>
        /// <param name="userFirstName">The first name of the user.</param>
        /// <param name="userEmailAddress">The email address of the user.</param>
        /// <param name="orgName">The user's organization.</param>
        /// <param name="orgDescription">A description of the user's organization.</param>
        /// <param name="addressDescription">A description of the address.</param>
        /// <param name="street">The street.</param>
        /// <param name="city">The city.</param>
        /// <param name="poBox">The PO box.</param>
        /// <param name="state">The state.</param>
        /// <param name="postalCode">The postal code.</param>
        /// <param name="country">The country.</param>
        public Document(string projectName, 
            string projectDescription = null, 
            string userId = null, 
            string userLastName = null, 
            string userFirstName = null, 
            string userEmailAddress = null, 
            string orgName = null, 
            string orgDescription = null,
            string addressDescription = null,
            string street = null,
            string city = null,
            string poBox = null,
            string state = null,
            string postalCode = null,
            string country = null)
        {
            // Create an organization for app creation.
            var appOrg = new IfcOrganization(APPNAME);
            this.AddEntity(appOrg);

            // Create an authoring application.
            var v = Assembly.GetExecutingAssembly().GetName().Version.ToString();
            var app = new IfcApplication(appOrg, v, APPNAME, APPNAME);
            this.AddEntity(app);

            var orgAddress = AddAddress(addressDescription, street, city, poBox, state, postalCode, country);
            var person = AddPerson(userId, userLastName, userFirstName, userEmailAddress, IfcRoleEnum.ARCHITECT);
            var org = AddOrganization(orgName, orgDescription, orgAddress);

            // Create an person and history for the owner history.
            var personAndOrg = new IfcPersonAndOrganization(person, org);
            this.AddEntity(personAndOrg);

            // Create an owner history for the project.
            var history = new IfcOwnerHistory(personAndOrg, app, IfcChangeActionEnum.ADDED, UnixNow());
            this.AddEntity(history);
            
            var unitAss = AddUnitAssignment();

            var geo = AddGeometricContext();

            // Create the project.
            var proj = new IfcProject(IfcGuid.ToIfcGuid(Guid.NewGuid()), history, projectName, projectDescription, null, null, null, new List<IfcRepresentationContext>{geo}, unitAss);   
            this.AddEntity(proj);
        }

        /// <summary>
        /// Create a document given a STEP file.
        /// </summary>
        /// <param name="STEPfilePath">The path to the STEP file.</param>
        /// <param name="errors">A list of errors generated during creation of the Document.</param>
        /// <returns>A Model.</returns>
        /// <exception cref="FileNotFoundException">The specified file path does not exist.</exception>
        public Document(string STEPfilePath, out List<STEPError> errors)
        {
            if (!File.Exists(STEPfilePath))
            {
                throw new FileNotFoundException($"The specified IFC STEP file does not exist: {STEPfilePath}.");
            }

            errors = new List<STEPError>();

            using (FileStream fs = new FileStream(STEPfilePath, FileMode.Open))
            {
                try 
                {
                    var sw = new Stopwatch();
                    sw.Start();

                    var input = new AntlrInputStream(fs);
                    var lexer = new STEP.STEPLexer(input);
                    var tokens = new CommonTokenStream(lexer);

                    var parser = new STEP.STEPParser(tokens);
                    parser.BuildParseTree = true;

                    var tree = parser.file();
                    var walker = new ParseTreeWalker();

                    var listener = new STEP.STEPListener();
                    walker.Walk(listener, tree);

                    sw.Stop();
                    Console.WriteLine($"{sw.Elapsed} for parsing STEP file {STEPfilePath}.");
                    sw.Reset();

                    sw.Start();
                    foreach (var data in listener.InstanceData)
                    {
                        if (listener.InstanceData[data.Key].ConstructedInstance != null)
                        {
                            // Instance may have been previously constructed as the result
                            // of another construction.
                            continue;
                        }

                        ConstructAndStoreInstance(data.Value, listener.InstanceData, data.Key, errors, 0);
                    }

                    // Transfer the constructed instances to storage.
                    foreach (var data in listener.InstanceData)
                    {
                        var inst = (BaseIfc)data.Value.ConstructedInstance;
                        storage.Add(inst.BaseId, inst);
                    }

                    sw.Stop();
                    Console.WriteLine($"{sw.Elapsed} for creating instances.");
                }
                catch(STEPUnknownSchemaException ex)
                {
                    errors.Add(new UnknownSchemaError(ex.RequestedSchema));
                }
            }
        }

        public IfcGeometricRepresentationContext AddGeometricContext()
        {
            //Ex: #38= IFCGEOMETRICREPRESENTATIONCONTEXT($,'Model',3,0.000010,#36,#37);
            var dimCount = new IfcDimensionCount(3);
            var location = new IfcCartesianPoint(new List<IfcLengthMeasure>{0,0,0});
            var up = new IfcDirection(new List<double>{0,0,1});
            var x= new IfcDirection(new List<double>{1,0,0});
            this.AddEntity(location);
            this.AddEntity(up);
            this.AddEntity(x);
            var place3d = new IfcAxis2Placement3D(location, up, x);
            var worldCs = new IfcAxis2Placement(place3d);
            var north = new IfcDirection(new List<double>{0,1,0});
            // this.AddEntity(worldCs);
            this.AddEntity(place3d);
            this.AddEntity(north);
            var geo = new IfcGeometricRepresentationContext(null, new IfcLabel("Model"), new IfcDimensionCount(3), 0.000010, worldCs, north);
            this.AddEntity(geo);
            return geo;
        }

        /// <summary>
        /// Add an entity to the document.
        /// </summary>
        /// <param name="entity">The entity to add to the document.</param>
        public void AddEntity(BaseIfc entity)
        {
            // Add the entity and recursively add all sub entities.
            if(this.storage.ContainsKey(entity.BaseId))
            {
                return;
            }

            this.storage.Add(entity.BaseId, entity);

            // Recursively store the entities referenced
            // in properties.
            // var t = entity.GetType();
            // var props = t.GetProperties();
            // foreach(var p in props)
            // {
            //     var val = p.GetValue(entity);
            //     if(val is BaseIfc)
            //     {
            //         AddEntity((BaseIfc)val);
            //     }
            // }
        }

        /// <summary>
        /// Write the document to STEP.
        /// </summary>
        /// <returns>A string representing the model serialized to STEP.</returns>
        public string ToSTEP(string filePath)
        {
            var sw = new Stopwatch();
            sw.Start();

            var builder = new StringBuilder();
            builder.AppendLine(Begin(filePath));

            var index = 1;
            foreach (var element in storage.Values)
            {
                element.StepId = index;
                index++;
            }

            foreach (var element in storage.Values)
            {
                var instanceValue = element.ToSTEP();
                builder.AppendLine(instanceValue);
            }

            builder.Append(End());

            sw.Stop();
            Console.WriteLine($"{sw.Elapsed} for serializing Document to STEP.");

            return builder.ToString();
        }

        /// <summary>
        /// Get all instances whose type derives from Tifc.
        /// </summary>
        /// <typeparam name="Tifc">A base type of the instances.</typeparam>
        /// <returns>A collection of BaseIfc.</returns>
        public IEnumerable<Tifc> AllInstancesDerivedFromType<Tifc>()
        {
            return storage.Values.Where(e=>typeof(Tifc).IsAssignableFrom(e.GetType())).Cast<Tifc>();
        }

        /// <summary>
        /// Get all instances of type Tifc.
        /// </summary>
        /// <typeparam name="Tifc">The type of the instances.</typeparam>
        /// <returns>A collection of BaseIfc.</returns>
        public IEnumerable<Tifc> AllInstancesOfType<Tifc>()
        {
            return storage.Values.Where(e=>e.GetType() == typeof(Tifc)).Cast<Tifc>();
        }

        /// <summary>
        /// Create an IfcUnitAssignment.
        /// </summary>
        /// <returns></returns>
        private IfcUnitAssignment AddUnitAssignment()
        {
            var lu = new IfcSIUnit(null, IfcUnitEnum.LENGTHUNIT, IfcSIUnitName.METRE);
            this.AddEntity(lu);
            var lengthUnit = new IfcUnit(lu);
            
            var au = new IfcSIUnit(null, IfcUnitEnum.AREAUNIT, IfcSIUnitName.SQUARE_METRE);
            this.AddEntity(au);
            var areaUnit = new IfcUnit(au);
            
            var vu = new IfcSIUnit(null, IfcUnitEnum.VOLUMEUNIT, IfcSIUnitName.CUBIC_METRE);
            this.AddEntity(vu);
            var volumeUnit = new IfcUnit(vu);

            var sau = new IfcSIUnit(null, IfcUnitEnum.SOLIDANGLEUNIT, IfcSIUnitName.STERADIAN);
            this.AddEntity(sau);
            var solidAngleUnit = new IfcUnit(sau);
            
            var mu = new IfcSIUnit(null, IfcUnitEnum.MASSUNIT, IfcSIUnitName.GRAM);
            this.AddEntity(mu);
            var massUnit = new IfcUnit(mu);

            var tu = new IfcSIUnit(null, IfcUnitEnum.TIMEUNIT, IfcSIUnitName.SECOND);
            this.AddEntity(tu);
            var timeUnit = new IfcUnit(tu);

            var thu = new IfcSIUnit(null, IfcUnitEnum.THERMODYNAMICTEMPERATUREUNIT, IfcSIUnitName.DEGREE_CELSIUS);
            this.AddEntity(thu);
            var thermUnit = new IfcUnit(thu);
            
            var lmu = new IfcSIUnit(null, IfcUnitEnum.LUMINOUSINTENSITYUNIT, IfcSIUnitName.LUMEN);
            this.AddEntity(lmu);
            var lumUnit = new IfcUnit(lmu);
            
            var pau = new IfcSIUnit(null, IfcUnitEnum.PLANEANGLEUNIT, IfcSIUnitName.RADIAN);
            this.AddEntity(pau);
            var planeAngleUnit = new IfcUnit(pau);
           
            var measure = new IfcMeasureWithUnit(new IfcValue(new IfcMeasureValue(new IfcPlaneAngleMeasure(1.745e-2))), planeAngleUnit);
            this.AddEntity(measure);

            var dimExp = new IfcDimensionalExponents(0,0,0,0,0,0,0);
            this.AddEntity(dimExp);

            var du = new IfcConversionBasedUnit(dimExp, IfcUnitEnum.PLANEANGLEUNIT, "DEGREE", measure);
            this.AddEntity(du);
            var degree = new IfcUnit(du);
            
            var units = new List<IfcUnit>{lengthUnit, areaUnit, volumeUnit, solidAngleUnit, massUnit, timeUnit, thermUnit, lumUnit, planeAngleUnit, degree};
            var unitAss = new IfcUnitAssignment(units);
            this.AddEntity(unitAss);

            return unitAss;
        }

        /// <summary>
        /// Create an IfcAddress.
        /// </summary>
        /// <param name="description"></param>
        /// <param name="street"></param>
        /// <param name="city"></param>
        /// <param name="poBox"></param>
        /// <param name="state"></param>
        /// <param name="postalCode"></param>
        /// <param name="country"></param>
        /// <returns></returns>
        private IfcPostalAddress AddAddress(string description, string street, string city, string poBox, string state, string postalCode, string country)
        {
            var lines = new List<IfcLabel>(){street};
            var address = new IfcPostalAddress(IfcAddressTypeEnum.OFFICE, description, null, null, lines, poBox, city, state, postalCode, country);
            this.AddEntity(address);
            return address;
        }

        /// <summary>
        /// Create an IfcPerson.
        /// </summary>
        /// <param name="userId"></param>
        /// <param name="lastName"></param>
        /// <param name="firstName"></param>
        /// <param name="emailAddress"></param>
        /// <param name="role"></param>
        /// <returns></returns>
        private IfcPerson AddPerson(string userId, string lastName, string firstName, string emailAddress, IfcRoleEnum role)
        {
            var r = new IfcActorRole(role);
            this.AddEntity(r);
            var person = new IfcPerson(userId, lastName, firstName, null, null, null, new List<IfcActorRole>{r}, null);
            this.AddEntity(person);
            return person;
        }

        /// <summary>
        /// Create an IfcOrganization.
        /// </summary>
        /// <param name="name"></param>
        /// <param name="description"></param>
        /// <param name="address"></param>
        /// <returns></returns>
        private IfcOrganization AddOrganization(string name, string description, IfcAddress address)
        {
            // Create an organization to own the Project
            var org = new IfcOrganization(name, name, description, 
                            new List<IfcActorRole>(), new List<IfcAddress>(){address});
            this.AddEntity(org);
            return org;
        }

        private string Begin(string filePath)
        {
            var project = AllInstancesOfType<IfcProject>().FirstOrDefault();
            var org = project != null ? project.OwnerHistory.OwningUser.TheOrganization.Name : new IfcLabel("Hypar");
            return $@"
ISO-10303-21;
HEADER;
FILE_DESCRIPTION(
	('ViewDefinition [CoordinationView]'),
	'2;1');
FILE_NAME(
    '{filePath}',
    '{DateTime.Now.ToString("yyyy-MM-ddTHH:MM:ss")}',
    ('{System.Environment.UserName}'),
    ('{org}'),
    'IFC-dotnet',
    '{typeof(Document).Assembly.GetName().Version}',
	'None');
FILE_SCHEMA (('IFC2X3'));
ENDSEC;
DATA;";
        }

        private string End()
        {
            return $@"ENDSEC;
END-ISO-10303-21;";
        }

        /// <summary>
        /// Recursively construct instances provided instance data.
        /// Construction is recursive because the instance data might include other
        /// instance data or id references to instances which have not yet been
        /// constructed.
        /// </summary>
        /// <param name="data">The instance data from which to construct the instance.</param>
        /// <param name="instances">The dictionary containing instance data gathered from the parser.</param>
        /// <param name="currLine"></param>
        /// <param name="errors"></param>
        /// <param name="level"></param>
        /// <returns></returns>
        private static object ConstructAndStoreInstance(STEP.InstanceData data, Dictionary<int, STEP.InstanceData> instances, int currLine, IList<STEPError> errors, int level)
        {
            var indent = string.Join("", Enumerable.Repeat("\t", level));

            //     Console.WriteLine($"{indent}{currLine},{data.Id} : Constructing type {data.Type.Name} with parameters [{string.Join(",",data.Parameters)}]");

            for (var i = 0; i < data.Parameters.Count(); i++)
            {
                if (data.Parameters[i] is STEP.InstanceData)
                {
                    data.Parameters[i] = ConstructAndStoreInstance((STEP.InstanceData)data.Parameters[i], instances, currLine, errors, level);
                }
                else if (data.Parameters[i] is STEP.STEPId)
                {
                    var stepId = data.Parameters[i] as STEP.STEPId;

                    // The instance has already been constructed.
                    // Use the id to look it up.
                    if (instances.ContainsKey(stepId.Value))
                    {
                        if (instances[stepId.Value].ConstructedInstance != null)
                        {
                            //Console.WriteLine($"{indent}Using pre-created instance {stepId.Value}");
                            data.Parameters[i] = instances[stepId.Value].ConstructedInstance;
                            continue;
                        }
                    }
                    // The instance's id cannot be found in the map.
                    // Log an error and set the parameter value to null.
                    else
                    {
                        errors.Add(new MissingIdError(currLine, stepId.Value));
                        data.Parameters[i] = null;
                        continue;
                    }
                    
                    data.Parameters[i] = ConstructAndStoreInstance(instances[stepId.Value], instances, currLine, errors, level);        
                }
                else if (data.Parameters[i] is List<object>)
                {
                    var list = data.Parameters[i] as List<object>;

                    // The parameters will have been stored in a List<object> during parsing.
                    // We need to create a List<T> where T is the type expected by the constructor
                    // in the STEP file.
                    var listType = typeof(List<>);
                    var instanceType = data.Constructor.GetParameters()[i].ParameterType.GetGenericArguments()[0];
                    var constructedListType = listType.MakeGenericType(instanceType);
                    var subInstances = (IList)Activator.CreateInstance(constructedListType);

                    if (!list.Any())
                    {
                        // Return our newly type empty list.
                        data.Parameters[i] = subInstances;
                        continue;
                    }

                    foreach (var item in list)
                    {
                        if (item is STEP.STEPId)
                        {
                            var id = item as STEP.STEPId;

                            // Do a check for an existing instance with this id.
                            if (instances.ContainsKey(id.Value))
                            {
                                if (instances[id.Value].ConstructedInstance != null)
                                {
                                    var existing = CoerceObject(instances[id.Value].ConstructedInstance, instanceType);
                                    subInstances.Add(existing);
                                    continue;
                                }
                            }
                            var subInstance = ConstructAndStoreInstance(instances[id.Value], instances, currLine, errors, level);
                            var coerce = CoerceObject(subInstance, instanceType);
                            subInstances.Add(coerce);
                        }
                        else if (item is STEP.InstanceData)
                        {
                            var subInstance = ConstructAndStoreInstance((STEP.InstanceData)item, instances, currLine, errors, level);
                            var coerce = CoerceObject(subInstance, instanceType);
                            subInstances.Add(coerce);
                        }
                        else
                        {
                            var subInstance = item;
                            var coerce = CoerceObject(subInstance, instanceType);
                            subInstances.Add(coerce);
                        }
                    }
                    // Replace the list of STEPId with a list of instance references.
                    data.Parameters[i] = subInstances;
                }
            }

            for(var i=0; i<data.Parameters.Count; i++)
            {
                data.Parameters[i] = CoerceObject(data.Parameters[i],data.Constructor.GetParameters()[i].ParameterType); 
            }
            
            // Construct the instance, assuming that all required sub-instances
            // have already been constructed.
            var instance = data.Constructor.Invoke(data.Parameters.ToArray());

            if (instance == null)
            {
                throw new Exception($"Could not construct an instance of {data.Constructor.DeclaringType} with parameters {data.Parameters}.");
            }

            // Inline instances will have an id of -1. Don't store these.
            // But DO return them to be used as constructor parameters.
            if (data.Id != -1)
            {
                instances[data.Id].ConstructedInstance = (BaseIfc)instance;
            }

            //Console.WriteLine($"Setting instanceDataMap[{data.Id}] constructed instance as {instance.Id} for type {instance.GetType().Name}.");
            return instance;
        }
        
        private static object CoerceObject(object value, Type to)
        {
            if(value == null)
            {
                return null;
            }

            var result = value;
            if(typeof(Select).IsAssignableFrom(to))
            {
                var ctorChain = new List<System.Reflection.ConstructorInfo>();
                System.Reflection.ConstructorInfo ctor = null;
                if(STEPListener.TypeHasConstructorForSelectChoice(to, value.GetType(), out ctor, ref ctorChain))
                {
                    result = ctor.Invoke(new object[]{value});
                    if(ctorChain.Any())
                    {
                        // Construct the necessary wrappers working
                        // backwards. For the first constructor, the parameter
                        // will be the constructed instance.
                        for(var y=ctorChain.Count-1; y>=0; y--)
                        {
                            result = ctorChain[y].Invoke(new object[]{result});
                        }
                    }
                }
            }
            return result; 
        }
    
        private int UnixNow()
        {
            return (Int32)(DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1))).TotalSeconds;
        }
    }
}