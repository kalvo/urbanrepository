#region -- REFERENCES ---------------------------------------------------------

using System;
using System.Text;
using System.Reflection;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using System.Drawing;
using System.IO;



using Rhino;
using Rhino.PlugIns;
using Rhino.Commands;
using Rhino.DocObjects;
using Rhino.Input;

using Rhino.Input.Custom;



[assembly: AssemblyTitle("UrbanRepository")]
[assembly: AssemblyDescription("")]
[assembly: AssemblyConfiguration("R5R5")]
[assembly: AssemblyCompany("")]
[assembly: AssemblyProduct("UrbanRepository")]
[assembly: AssemblyCopyright("Copyright © 2014 by Raul Kalvo")]
[assembly: AssemblyTrademark("")]
[assembly: AssemblyCulture("")]
[assembly: ComVisible(false)]
[assembly: Guid("89811ECA-5D40-47A4-A18A-0877F9EF03A2")]
[assembly: AssemblyVersion("1.0.0.0")]
[assembly: AssemblyFileVersion("1.0.0.0")]

[assembly: PlugInDescription(DescriptionType.Address, "no")]
[assembly: PlugInDescription(DescriptionType.Country, "Singapore")]
[assembly: PlugInDescription(DescriptionType.Email, "kalvo@inphysica.com")]
[assembly: PlugInDescription(DescriptionType.Phone, "+65 81685842")]
[assembly: PlugInDescription(DescriptionType.Fax, "N/A")]
[assembly: PlugInDescription(DescriptionType.Organization, "Urban Repository")]
[assembly: PlugInDescription(DescriptionType.UpdateUrl, "urbanrepository.net")]
[assembly: PlugInDescription(DescriptionType.WebSite, "urbanrepository.net")]

#endregion

namespace UR
{

    #region -- PLUGIN SYSTEM ----------------------------------------------------

    public class UrbanRepository : Rhino.PlugIns.PlugIn
    {

        public UrbanRepository()
            : base()
        {

        }

        protected override LoadReturnCode OnLoad(ref string errorMessage)
        {
            return LoadReturnCode.Success;
        }

    }

    #endregion

    #region -- PLUGIN COMMAND ---------------------------------------------------

    //[Guid("37E536C4-EA65-4A9F-98B9-DB1CDDCAC209")] // Do i need that?


    [Rhino.Commands.CommandStyle(Rhino.Commands.Style.ScriptRunner)] // Needed for every command that executes a rhino script/macro command.

    public partial class Command_openScene : Rhino.Commands.Command
    {
        public override string EnglishName
        {
            get
            {
                return "urOpenScene";
            }
        }

        protected override Result RunCommand(RhinoDoc doc, RunMode mode)
        {
            var path = RhinoGet.GetFileName(GetFileNameMode.OpenTextFile, "*.txt", "Select scene description file:", null);
            if (path == string.Empty || !System.IO.File.Exists(path))
                return Result.Cancel;

            urEngine.WorkingDir = path.Remove(path.LastIndexOf('\\') + 1);

            string line;
            string[] temp;
            Coord Referencepoint;
            System.IO.StreamReader file = new System.IO.StreamReader(path);

            // Read in FIRST LINE, the x coordinate

            if ((line = file.ReadLine()) != null)
            {
                temp = line.Split(' ');
                if (temp[0] == "X" & temp.Length == 2)
                {
                    try
                    {
                        Referencepoint.X = Convert.ToDouble(temp[1]);
                    }
                    catch (FormatException)
                    {
                        RhinoApp.WriteLine("Error in scene X ref.");
                        return Result.Failure;
                    }
                }
                else
                {
                    RhinoApp.WriteLine("Error in 1st line formating.");
                    return Result.Failure;
                }
            }
            else
            {
                RhinoApp.WriteLine("Error in 1st line formating.");
                return Result.Failure;
            }


            // Read in SECOND LINE, the y coordinate

            if ((line = file.ReadLine()) != null)
            {
                temp = line.Split(' ');
                if (temp[0] == "Y" & temp.Length == 2)
                {
                    try
                    {
                        Referencepoint.Y = Convert.ToDouble(temp[1]);
                    }
                    catch (FormatException)
                    {
                        RhinoApp.WriteLine("Error in scene Y ref.");
                        return Result.Failure;
                    }
                }
                else
                {
                    RhinoApp.WriteLine("Error in 2nd line formating.");
                    return Result.Failure;
                }
            }
            else
            {
                RhinoApp.WriteLine("Error in 2nd line formating.");
                return Result.Failure;
            }

            // Read in THIRD LINE, the number of buildings, the exact number does not matter
            line = file.ReadLine();

            //set Coordinate system
            Referencepoint.Z = 0;
            CoordinateSystem.WorldRef = Referencepoint;
            CoordinateSystem.DocRefPoint = new Rhino.Geometry.Point3d(0, 0, 0);
            CoordinateSystem.IsDirty = false;

            while ((line = file.ReadLine()) != null)
            {
                if (line != "")
                {
                    RhinoApp.WriteLine("Loading building: " + line);
                    urEngine.LoadBuilding(line);
                }
            }
            return Result.Success;
        }
    }

    [Rhino.Commands.CommandStyle(Rhino.Commands.Style.ScriptRunner)]

    public partial class Command_buildingOpen : Rhino.Commands.Command
    {
        public override string EnglishName
        {
            get
            {
                return "urBuildingOpen";
            }
        }

        protected override Result RunCommand(RhinoDoc doc, RunMode mode)
        {

            if (CoordinateSystem.IsDirty || urEngine.WorkingDir == null)
            {
                RhinoApp.WriteLine("Reference point not set, please use urSetRef command or open a scene.");
                return Result.Failure;
            }

            //ask for identifier
            string identifier = "";
            if (RhinoGet.GetString("Insert model identifier:", false, ref identifier) != Result.Success)
                return Result.Failure;

            //  check that not taken
            string directoryToLoad = System.IO.Path.Combine(urEngine.WorkingDir, identifier);
            if (!Directory.Exists(directoryToLoad))
            {
                RhinoApp.WriteLine("Model \"{0}\" not in the repository.", identifier);
                return Result.Failure;
            }

            if (urEngine.LoadBuilding(identifier) != Result.Success)
            {
                RhinoApp.WriteLine("Error loading building \"{0}\".", identifier);
                return Result.Failure;
            }
            return Result.Success;
        }
    }

    [Rhino.Commands.CommandStyle(Rhino.Commands.Style.ScriptRunner)]

    public partial class Command_buildingNew : Rhino.Commands.Command
    {
        public override string EnglishName
        {
            get
            {
                return "urBuildingNew";
            }
        }

        protected override Result RunCommand(RhinoDoc doc, RunMode mode)
        {
            //Check if scene open
            if (CoordinateSystem.IsDirty)
            {
                RhinoApp.WriteLine("Reference point not set, please use SetRef command or open a scene.");
                return Result.Failure;
            }

            //ask for obj file
            var filepath = RhinoGet.GetFileName(GetFileNameMode.Import, "*.obj", "Select model .obj file:", null);
            if (filepath == string.Empty || !System.IO.File.Exists(filepath))
                return Result.Cancel;

            //ask for ident
            string identifier = "";
            if (RhinoGet.GetString("Insert model identifier:", false, ref identifier) != Result.Success)
                return Result.Failure;

            //  check that not taken
            string newpath = System.IO.Path.Combine(urEngine.WorkingDir, identifier);
            if (Directory.Exists(newpath))
            {
                RhinoApp.WriteLine("Identifier \"{0}\" already taken", identifier);
                return Result.Failure;
            }

            //create folder
            Directory.CreateDirectory(newpath);

            //copy obj
            string targetFile = System.IO.Path.Combine(newpath, identifier + ".obj");
            System.IO.File.Copy(filepath, targetFile);

            //create metadata (center3d -> SceneReference point)
            string metadataFilePath = System.IO.Path.Combine(newpath, identifier + ".txt");
            string[] metadata = {"#coordinate_system:L-EST97", 
                                    "#height_system:Balti77", 
                                    "#center3d: "+ CoordinateSystem.WorldRef.X+","+ CoordinateSystem.WorldRef.Y+","+ CoordinateSystem.WorldRef.Z, 
                                    "", 
                                    "#tag:--"};

            System.IO.File.WriteAllLines(metadataFilePath, metadata);
            //load model
            urEngine.LoadBuilding(identifier);
            return Result.Success;
        }
    }

    public partial class Command_buildingResave : Rhino.Commands.Command
    {
        public override string EnglishName
        {
            get
            {
                return "urBuildingResave";
            }
        }

        protected override Result RunCommand(RhinoDoc doc, RunMode mode)
        {
            if (CoordinateSystem.IsDirty || urEngine.WorkingDir == null)
            {
                RhinoApp.WriteLine("Reference point not set, please use SetRef command or open a scene.");
                return Result.Failure;
            }

            //ask to select building
            Rhino.DocObjects.ObjRef modelToUpdateRef;
            if (RhinoGet.GetOneObject("Select model to update:", false, Rhino.DocObjects.ObjectType.InstanceReference, out modelToUpdateRef) != Result.Success)
            {
                RhinoApp.WriteLine("Error selecting a model. Please try again.");
                return Result.Failure;
            }

            //get block insertation point
            Rhino.DocObjects.InstanceObject modelToUpdate = modelToUpdateRef.Object() as Rhino.DocObjects.InstanceObject;
            if (modelToUpdate == null)
            {
                RhinoApp.WriteLine("Error in model reference.");
                return Result.Failure;
            }
            Rhino.Geometry.Point3d pt = modelToUpdate.InsertionPoint;
            Coord newCoord;
            CoordinateSystem.CoordToLoc(pt, out newCoord);

            //get identifier
            string modelIdentifier = modelToUpdate.InstanceDefinition.Name;

            //open model metadata file
            string metadataPath = System.IO.Path.Combine(urEngine.WorkingDir, modelIdentifier, modelIdentifier + ".txt");
            StringBuilder newFile = new StringBuilder();
            string temp = "";

            string[] file = File.ReadAllLines(metadataPath);

            foreach (string line in file)
            {
                if (line.Split(':')[0] == "#center3d")
                {
                    temp = "#center3d: " + newCoord.X + "," + newCoord.Y + "," + newCoord.Z;
                    newFile.Append(temp + "\r\n");
                }
                else
                {
                    newFile.Append(line + "\r\n");
                }
            }

            File.WriteAllText(metadataPath, newFile.ToString());

            return Result.Success;
        }
    }

    public partial class Command_buildingUnload : Rhino.Commands.Command
    {
        public override string EnglishName
        {
            get
            {
                return "urBuildingUnload";
            }
        }

        protected override Result RunCommand(RhinoDoc doc, RunMode mode)
        {

            //ask to select building
            Rhino.DocObjects.ObjRef modelToUnloadRef;
            if (RhinoGet.GetOneObject("Select model to unload:", false, Rhino.DocObjects.ObjectType.InstanceReference, out modelToUnloadRef) != Result.Success)
            {
                RhinoApp.WriteLine("Error selecting a model. Please try again.");
                return Result.Failure;
            }


            Rhino.DocObjects.InstanceObject modelToUnload = modelToUnloadRef.Object() as Rhino.DocObjects.InstanceObject;
            if (modelToUnload == null)
            {
                RhinoApp.WriteLine("Error in model reference.");
                return Result.Failure;
            }

            //get identifier
            string modelIdentifier = modelToUnload.InstanceDefinition.Name;

            urEngine.UnloadBuilding(modelIdentifier);

            return Result.Success;
        }
    }

    public partial class Command_setRef : Rhino.Commands.Command
    {
        public override string EnglishName
        {
            get
            {
                return "urSetRef";
            }
        }

        protected override Result RunCommand(RhinoDoc doc, RunMode mode)
        {

            Rhino.Geometry.Point3d p;
            Result rc;

            double WorldX = -1;
            double WorldY = -1;
            double WorldZ = -1;

            if (!CoordinateSystem.IsDirty)
            {

                WorldX = CoordinateSystem.WorldRef.X;
                WorldY = CoordinateSystem.WorldRef.Y;
                WorldZ = CoordinateSystem.WorldRef.Z;

            }

            CoordinateSystem.buildUnitConversion(doc);

            rc = Rhino.Input.RhinoGet.GetPoint("Pick a reference location", true, out p);

            if (p == null) return Result.Success;

            // X Value (Down-Up direction, Latitude)
            rc = Rhino.Input.RhinoGet.GetNumber("X [m] in L-EST (between 6 370 000m and 6 640 000m)", true, ref WorldX, 6370000, 6640000);
            if (rc == Result.Nothing) return Result.Success;

            // Y Value (Left-Right direction, Longitude )
            rc = Rhino.Input.RhinoGet.GetNumber("Y [m] in L-EST (between 367 400m and 740 000m)", true, ref WorldY, 367400, 740000);
            if (rc == Result.Nothing) return Result.Success;

            // Z Value (), Altitude
            rc = Rhino.Input.RhinoGet.GetNumber("Z [m] in L-EST (between 0m and 400m)", true, ref WorldZ, 0, 400);
            if (rc == Result.Nothing) return Result.Success;

            CoordinateSystem.WorldRef.X = WorldX;
            CoordinateSystem.WorldRef.Y = WorldY;
            CoordinateSystem.WorldRef.Z = WorldZ;

            CoordinateSystem.DocRefPoint = p;

            CoordinateSystem.IsDirty = false;

            Rhino.Display.DisplayPipeline.DrawForeground += CoordinateSystem.e.DoCoordinateDrawing;

            return Result.Success;


        }
    }

    public partial class Command_getObject : Rhino.Commands.Command
    {
        public override string EnglishName
        {
            get
            {
                return "urGetObject";
            }
        }

        protected override Result RunCommand(RhinoDoc doc, RunMode mode)
        {

            Rhino.Geometry.Point3d OriginPoint;
            Rhino.Geometry.Point3d PointOnX;
            Result rc;

            if (CoordinateSystem.IsDirty)
            {
                RhinoApp.WriteLine("Reference coordinate system needs to be set before. Use urSetRef command before");
                return Result.Success;
            }

            rc = Rhino.Input.RhinoGet.GetPoint("Pick object file origin (location of [0,0,0] coordinate in object file.)", true, out OriginPoint);
            if (rc == Result.Nothing) return Result.Success;

            CoordinateSystem.ObjFrameOrigin = OriginPoint;

            Rhino.Input.Custom.GetPoint gp = new Rhino.Input.Custom.GetPoint();

            gp.DynamicDraw += CoordinateSystem.e.DrawFrame;

            while (true)
            {
                var result = gp.Get();
                if (result == Rhino.Input.GetResult.Cancel) return Result.Success;
                if (result == Rhino.Input.GetResult.Point)
                {
                    PointOnX = gp.Point();
                    break;
                }
            }


            bool isValidConf = CoordinateSystem.buildUnitConversion(doc);

            if (!isValidConf)
            {

                RhinoApp.WriteLine("Document units {0} are not supported", doc.ModelUnitSystem.ToString());
                return Result.Success;

            }

            // Rotation:

            Rhino.Geometry.Vector3d vx = new Rhino.Geometry.Vector3d(1, 0, 0);

            PointOnX.Z = OriginPoint.Z;

            Rhino.Geometry.Vector3d ObjectXVector = new Rhino.Geometry.Vector3d(PointOnX.X - OriginPoint.X, PointOnX.Y - OriginPoint.Y, 0);

            Rhino.Geometry.Plane p = new Rhino.Geometry.Plane(new Rhino.Geometry.Point3d(0, 0, 0), new Rhino.Geometry.Vector3d(1, 0, 0), new Rhino.Geometry.Vector3d(0, 1, 0));

            double aRad = Rhino.Geometry.Vector3d.VectorAngle(vx, ObjectXVector, p);

            double aDegree = aRad * 180 / Math.PI;

            RhinoApp.WriteLine(" Rad:{0:0.000}, Degrees: {1:0.000} ", aRad, aDegree);

            // Shift

            double ObjRefX = CoordinateSystem.WorldRef.X + (CoordinateSystem.ObjFrameOrigin.Y - CoordinateSystem.DocRefPoint.Y) * CoordinateSystem.DocUnitsToMeters;
            double ObjRefY = CoordinateSystem.WorldRef.Y + (CoordinateSystem.ObjFrameOrigin.X - CoordinateSystem.DocRefPoint.X) * CoordinateSystem.DocUnitsToMeters;
            double ObjRefZ = CoordinateSystem.WorldRef.Z + (CoordinateSystem.ObjFrameOrigin.Z - CoordinateSystem.DocRefPoint.Z) * CoordinateSystem.DocUnitsToMeters;

            RhinoApp.WriteLine(" Wy:{0:0.000} \t Wx:{1:0.000} \t Wz: {2:0.000} ", ObjRefY, ObjRefX, ObjRefZ);


            return Result.Success;


        }
    }

    public partial class Command_version : Rhino.Commands.Command
    {
        public override string EnglishName
        {
            get
            {
                return "urVersion";
            }
        }

        protected override Result RunCommand(RhinoDoc doc, RunMode mode)
        {


            RhinoApp.WriteLine("");
            RhinoApp.WriteLine("Date:  2014-07-19C");
            RhinoApp.WriteLine("Status:  Pre-alpha (Milestone)");
            RhinoApp.WriteLine("Version:  0.0.2");
            RhinoApp.WriteLine("");
            RhinoApp.WriteLine("Author: kalvo@inphysica.com (Raul Kalvo)");
            RhinoApp.WriteLine("        taavilooke@gmail.com (Taavi Lõoke)");
            RhinoApp.WriteLine("Licensed to:  Estonia Academy of Arts (2014)");
            RhinoApp.WriteLine("");

            return Result.Success;


        }
    }


    #endregion

    #region -- CLASS --

    static class urEngine
    {
        public static string WorkingDir;

        static public Result LoadBuilding(string id)
        {
            //parse metadata
            Coord loc = new Coord();
            Boolean LocFound = false;
            string line;

            using (System.IO.StreamReader file = new System.IO.StreamReader(WorkingDir + id + '\\' + id + ".txt"))
            {
                while ((line = file.ReadLine()) != null)
                {
                    if (line != "")
                    {
                        string[] temp = line.Split(':');
                        //RhinoApp.WriteLine("Read from metadata:" + line);
                        //RhinoApp.WriteLine("    Tag part of line:" + temp[0]);
                        switch (temp[0])
                        {
                            case "#center3d":
                                //RhinoApp.WriteLine("    Center point line detected");
                                if (LocFound)
                                {
                                    return Result.Failure;
                                }

                                if (temp.Length != 2)
                                {
                                    return Result.Failure;
                                }

                                string[] temp1 = temp[1].Trim().Split(',');

                                //RhinoApp.WriteLine("Points: X" + temp1[0] + " Y" + temp1[1] + " Z" + temp1[2]);

                                if (temp1.Length != 3)
                                {
                                    RhinoApp.WriteLine("Error in coorinates line format.");
                                    return Result.Failure;
                                }

                                try
                                {
                                    loc.X = Convert.ToDouble(temp1[0]);
                                    loc.Y = Convert.ToDouble(temp1[1]);
                                    loc.Z = Convert.ToDouble(temp1[2]);
                                }
                                catch (FormatException)
                                {
                                    RhinoApp.WriteLine("Error converting coordinates to numbers.");
                                    return Result.Failure;
                                }

                                LocFound = true;
                                break;
                        }
                    }
                }
                file.ReadToEnd();
            }

            if (!LocFound)
            {
                RhinoApp.WriteLine("Insertation coordinates not found in file");
                return Result.Failure;
            }
            //RhinoApp.WriteLine("    Location read: " + loc.X + " " + loc.Y + " " + loc.Z);
            Rhino.Geometry.Point3d InsertationPoint;
            if (CoordinateSystem.LocToCoord(loc, out InsertationPoint) != Result.Success)
            {
                RhinoApp.WriteLine("Error in finding insertation point, something wrong with the reference point");
                return Result.Failure;
            }
            //RhinoApp.WriteLine("  Insertation Point: " + InsertationPoint.X + " " + InsertationPoint.Y + " " + InsertationPoint.Z);
            Rhino.RhinoApp.RunScript("!_-Insert File=Yes LinkMode=Link " + WorkingDir + id + '\\' + id + ".obj"
                + " Block ImportOBJGroups=AsLayers ImportOBJObjects=Yes ReverseGroupOrder=No ImportAsMorphTargetOnly=No IgnoreTextures=No MapObjYtoRhinoZ=No _Enter "
                + InsertationPoint.X + "," + InsertationPoint.Y + "," + InsertationPoint.Z + " 1 0", false);
            // How to detect if the model actually loaded?
            RhinoApp.WriteLine("Model loaded.");
            return Result.Success;
        }

        static public Result UnloadBuilding(string id)
        {
            RhinoDoc doc = RhinoDoc.ActiveDoc;

            Rhino.DocObjects.InstanceDefinition DefToUnload = doc.InstanceDefinitions.Find(id, true);

            doc.InstanceDefinitions.Delete(DefToUnload.Index, true, false);

            return Result.Success;
        }
    }

    static class CoordinateSystem
    {

        public static Events e = new Events();

        public static bool IsDirty = true;
        public static Rhino.Geometry.Point3d DocRefPoint;
        public static Coord WorldRef;

        public static Rhino.Geometry.Point3d ObjFrameOrigin;

        public static double DocUnitsToMeters = 1;

        static public bool buildUnitConversion(RhinoDoc doc)
        {

            double conf = -1;

            switch (doc.ModelUnitSystem)
            {

                case UnitSystem.Kilometers:
                    conf = 1000;
                    break;

                case UnitSystem.Meters:
                    conf = 1;
                    break;

                case UnitSystem.Decimeters:
                    conf = 0.1;
                    break;

                case UnitSystem.Centimeters:
                    conf = 0.01;
                    break;

                case UnitSystem.Millimeters:
                    conf = 0.001;
                    break;

            }

            if (conf == -1)
            {
                return false;
            }
            else
            {

                DocUnitsToMeters = conf;
                return true;

            }


        }

        static public bool inBounds(Coord loc) // Checks if the coordinate is in Estonia
        {
            if (loc.X > 6640000 | loc.X < 6370000)
                return false;
            if (loc.Y < 367400 | loc.Y > 740000)
                return false;
            if (loc.Z < 0 | loc.Z > 400)
                return false;

            return true;
        }

        // Converts a L-EST coordinate to a point in Rhino model according to setRef coordinates
        static public Result LocToCoord(Coord loc, out Rhino.Geometry.Point3d output)
        {
            if (IsDirty)
            {
                output = new Rhino.Geometry.Point3d();
                return Result.Failure;
            }


            if (!inBounds(loc))
            {
                output = new Rhino.Geometry.Point3d();
                return Result.Failure;
            }

            Rhino.Geometry.Vector3d diffvector = new Rhino.Geometry.Vector3d(loc.Y - WorldRef.Y, loc.X - WorldRef.X, loc.Z - WorldRef.Z);
            output = Rhino.Geometry.Point3d.Add(DocRefPoint, diffvector);
            return Result.Success;
        }


        // Converts a point in Rhino model to L-EST coordinate according to setRef coordinates
        static public Result CoordToLoc(Rhino.Geometry.Point3d coord1, out Coord output)
        {
            if (IsDirty)
            {
                output.X = 0;
                output.Y = 0;
                output.Z = 0;
                return Result.Failure;
            }

            Rhino.Geometry.Vector3d diffvector = Rhino.Geometry.Point3d.Subtract(coord1, DocRefPoint);
            output.X = WorldRef.X + diffvector.Y;
            output.Y = WorldRef.Y + diffvector.X;
            output.Z = WorldRef.Z + diffvector.Z;
            return Result.Success;
        }

    }

    struct Coord
    {

        public double X;
        public double Y;
        public double Z;

    }

    class Events
    {

        public void DoCoordinateDrawing(object sender, Rhino.Display.DrawEventArgs e)
        {

            if (CoordinateSystem.IsDirty) return;

            double c = 1 / CoordinateSystem.DocUnitsToMeters;

            Rhino.Geometry.Line xAxis = new Rhino.Geometry.Line(CoordinateSystem.DocRefPoint, CoordinateSystem.DocRefPoint + new Rhino.Geometry.Vector3d(1, 0, 0) * c);
            Rhino.Geometry.Line yAxis = new Rhino.Geometry.Line(CoordinateSystem.DocRefPoint, CoordinateSystem.DocRefPoint + new Rhino.Geometry.Vector3d(0, 1, 0) * c);
            Rhino.Geometry.Line zAxis = new Rhino.Geometry.Line(CoordinateSystem.DocRefPoint, CoordinateSystem.DocRefPoint + new Rhino.Geometry.Vector3d(0, 0, 1) * c);

            e.Display.DrawLineArrow(xAxis, Color.Red, 2, 0.1 * c);
            e.Display.DrawLineArrow(yAxis, Color.Green, 2, 0.1 * c);
            e.Display.DrawLineArrow(zAxis, Color.Blue, 2, 0.1 * c);

            Rhino.Display.Text3d tx = new Rhino.Display.Text3d(string.Format(" Wx:\t{0:0.000}m(green) \n Wy:\t{1:0.000}m(red) \n Wz:\t{2:0.000}m(blue)", CoordinateSystem.WorldRef.X, CoordinateSystem.WorldRef.Y, CoordinateSystem.WorldRef.Z), new Rhino.Geometry.Plane(CoordinateSystem.DocRefPoint - new Rhino.Geometry.Vector3d(0, 0.4, 0) * c, new Rhino.Geometry.Vector3d(0, 0, 1)), 0.2 * c);
            e.Display.Draw3dText(tx, Color.Black);

        }

        public void DrawFrame(object sender, Rhino.Input.Custom.GetPointDrawEventArgs e)
        {

            double c = 1 / CoordinateSystem.DocUnitsToMeters;

            Rhino.Geometry.Point3d PointOnX = new Rhino.Geometry.Point3d(e.CurrentPoint);
            PointOnX.Z = CoordinateSystem.ObjFrameOrigin.Z;

            Rhino.Geometry.Vector3d XVector = new Rhino.Geometry.Vector3d(PointOnX.X - CoordinateSystem.ObjFrameOrigin.X, PointOnX.Y - CoordinateSystem.ObjFrameOrigin.Y, 0);
            XVector.Unitize();

            Rhino.Geometry.Vector3d ZVector = new Rhino.Geometry.Vector3d(0, 0, 1);

            Rhino.Geometry.Vector3d YVector = Rhino.Geometry.Vector3d.CrossProduct(ZVector, XVector);

            Rhino.Geometry.Line xAxis = new Rhino.Geometry.Line(CoordinateSystem.ObjFrameOrigin, CoordinateSystem.ObjFrameOrigin + XVector * c);
            Rhino.Geometry.Line yAxis = new Rhino.Geometry.Line(CoordinateSystem.ObjFrameOrigin, CoordinateSystem.ObjFrameOrigin + YVector * c);
            Rhino.Geometry.Line zAxis = new Rhino.Geometry.Line(CoordinateSystem.ObjFrameOrigin, CoordinateSystem.ObjFrameOrigin + ZVector * c);

            e.Display.DrawLineArrow(xAxis, Color.Red, 2, 0.1 * c);
            e.Display.DrawLineArrow(yAxis, Color.Green, 2, 0.1 * c);
            e.Display.DrawLineArrow(zAxis, Color.Blue, 2, 0.1 * c);


        }


    }

    #endregion

}

