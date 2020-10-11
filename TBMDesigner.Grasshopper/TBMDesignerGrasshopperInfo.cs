using System;
using System.Drawing;
using Grasshopper.Kernel;

namespace TBMDesigner.Grasshopper
{
    public class TBMDesignerGrasshopperInfo : GH_AssemblyInfo
  {
    public override string Name
    {
        get
        {
            return "TBMDesignerGrasshopper";
        }
    }
    public override Bitmap Icon
    {
        get
        {
            //Return a 24x24 pixel bitmap to represent this GHA library.
            return null;
        }
    }
    public override string Description
    {
        get
        {
            //Return a short string describing the purpose of this GHA library.
            return "TBM Designer Test";
        }
    }
    public override Guid Id
    {
        get
        {
            return new Guid("ab69e617-4989-4165-b324-53fb45234246");
        }
    }

    public override string AuthorName
    {
        get
        {
            //Return a string identifying you or your company.
            return "Peggy Lin";
        }
    }
    public override string AuthorContact
    {
        get
        {
            //Return a string representing your preferred contact details.
            return "";
        }
    }

}
}
