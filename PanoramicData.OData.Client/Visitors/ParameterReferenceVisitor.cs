namespace PanoramicData.OData.Client.Visitors;

internal sealed class ParameterReferenceVisitor : ExpressionVisitor
{
	public bool FoundParameter { get; private set; }

	protected override Expression VisitParameter(ParameterExpression node)
	{
		FoundParameter = true;
		return base.VisitParameter(node);
	}
}