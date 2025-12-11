using System.Collections.Generic;

namespace Dynamite3D.RealIvy
{
	[System.Serializable]
	public class BranchSegment
	{
		public List<LeafPoint> leaves;
		public BranchPoint initSegment;
		public BranchPoint endSegment;
	}
}