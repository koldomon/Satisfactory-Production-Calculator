Imports System.Xml.Serialization
<DebuggerDisplay("Res: {Name} [{Type}]")> Public Class Resource
    Implements IEquatable(Of Resource)

    Private _ProductionCost As Double?
    Private _ProductionRate As Double?
    Private _GlobalSourcesSum As Double?

    Private Shared _AllResources As List(Of Resource)
    Private Shared _AllResourcesSum As Double?

    <XmlAttribute> Public Property Name As String
    <XmlAttribute> Public Property Type As String
    <XmlAttribute> Public Property Category As String
    <XmlAttribute> Public Property ProductionPerMinute As Double
    <XmlAttribute> Public Property ProductionPerMachine As Double
    <XmlAttribute> Public Property IsAlternativeRecipe As Boolean
    <XmlAttribute> Public Property ItemsPerMinute As Double
    <XmlAttribute> Public Property GlobalSourcesType As String
    <XmlAttribute> Public Property GlobalSourcesCount As Double
    <XmlArray(ElementName:="AdditionalProductions", IsNullable:=False), XmlArrayItem(Type:=GetType(Resource), ElementName:="Resource")> Public Property AdditionalResources As List(Of Resource)
    <XmlArray(ElementName:="Resources", IsNullable:=False), XmlArrayItem(Type:=GetType(Resource), ElementName:="Resource")> Public Property Resources As List(Of Resource)
    <XmlArray(ElementName:="GlobalSources", IsNullable:=False), XmlArrayItem(Type:=GetType(Resource), ElementName:="Resource")> Public Property GlobalSources As List(Of Resource)
    <XmlIgnore> Public ReadOnly Property GlobalSourcesSum As Double
        Get
            If (Not _GlobalSourcesSum.HasValue) Then
                If (Me.GlobalSources IsNot Nothing) AndAlso (Me.GlobalSources.Any) Then
                    If (Not String.IsNullOrEmpty(Me.Name)) AndAlso (Me.Name.Equals("Crude Oil")) Then
                        _GlobalSourcesSum = Me.GlobalSources.Sum(Function(x) [Enum].Parse(GetType(ResourceNodeTypeEnum), x.GlobalSourcesType) * 2 * x.GlobalSourcesCount)
                    Else
                        _GlobalSourcesSum = Me.GlobalSources.Sum(Function(x) [Enum].Parse(GetType(ResourceNodeTypeEnum), x.GlobalSourcesType) * x.GlobalSourcesCount)
                    End If
                ElseIf (Not String.IsNullOrEmpty(Me.Name)) AndAlso (Me.Name.Equals("Water")) Then
                    _GlobalSourcesSum = ResourceNodeTypeEnum.Pure * 100
                Else
                    _GlobalSourcesSum = 0
                End If
            End If

            Return _GlobalSourcesSum.Value
        End Get
    End Property
    Public Function GetGlobalSourcesSum(thisMinerType As MinerTypeEnum, thisMinerSpeed As MinerSpeedEnum, thisBeltSpeed As BeltSpeedEnum) As Double
        Dim myReturn As Double
        If (Me.GlobalSources IsNot Nothing) AndAlso (Me.GlobalSources.Any) Then
            For Each mySource In Me.GlobalSources
                Dim myBaseValue As Double = ResourceNodeTypeEnum.Normal
                Dim myTempValue As Double

                If (Me.Name.Equals("Crude Oil")) OrElse (Me.Name.Equals("Water")) Then myBaseValue = 120

                Select Case DirectCast([Enum].Parse(GetType(ResourceNodeTypeEnum), mySource.GlobalSourcesType), ResourceNodeTypeEnum)
                    Case ResourceNodeTypeEnum.Impure
                        myTempValue = myBaseValue / 2 * thisMinerType * (thisMinerSpeed / 100)
                    Case ResourceNodeTypeEnum.Normal
                        myTempValue = myBaseValue * thisMinerType * (thisMinerSpeed / 100)
                    Case ResourceNodeTypeEnum.Pure
                        myTempValue = myBaseValue * 2 * thisMinerType * (thisMinerSpeed / 100)
                End Select

                If (Me.Name.Equals("Crude Oil")) OrElse (Me.Name.Equals("Water")) Then
                    If (myTempValue > 300) Then myTempValue = 300 'cap fluids by max output
                Else
                    If (myTempValue > thisBeltSpeed) Then myTempValue = thisBeltSpeed 'cap resource by max belt speed
                End If
                myReturn += myTempValue * mySource.GlobalSourcesCount
            Next
        End If

        Return myReturn
    End Function
    <XmlIgnore> Friend Shared Property AllResources As List(Of Resource)
        Get
            If (_AllResources Is Nothing) Then _AllResources = New List(Of Resource)

            Return _AllResources
        End Get
        Set
            _AllResources = Value
        End Set
    End Property

    <XmlIgnore> Public ReadOnly Property ProductionCost As Double
        Get
            If (Not _ProductionCost.HasValue) Then
                _ProductionCost = GetAllResourceInputs() / Me.ProductionRate
                If _ProductionCost > 1 Then _ProductionCost = Math.Round(_ProductionCost.Value, 1, MidpointRounding.AwayFromZero)
            End If

            Return _ProductionCost.Value
        End Get
    End Property

    Private Function GetAllResourceInputs() As Double
        Dim myReturn As Double = 0

        If (Me.GlobalSourcesSum > 0) Then
            myReturn = 1 / Me.GlobalSourcesSum
        ElseIf (Me.Resources IsNot Nothing) AndAlso (Me.Resources.Any) Then
            For Each myResource In Me.Resources
                Dim myOrgResource = Resource.AllResources.Where(Function(x) x.Name.StartsWith(myResource.Type) AndAlso (x.Type = myResource.Type)).OrderBy(Function(x) x.ProductionCost).FirstOrDefault

                Dim myTmp = Resource.AllResources.Where(Function(x) (x.Name.StartsWith(myResource.Type)) AndAlso (x.Type = myResource.Type) AndAlso (x.Category = myOrgResource.Category)).OrderBy(Function(x) x.ProductionCost).FirstOrDefault
                If (myTmp IsNot Nothing) Then
                    Dim myVal = myResource.ItemsPerMinute * myTmp.GetAllResourceInputs
                    myReturn += myVal
                End If
            Next
        Else
            myReturn = Me.ProductionRate
        End If

        Return myReturn
    End Function

    Public ReadOnly Property ProductionRate() As Double
        Get
            If (Not _ProductionRate.HasValue) Then
                _ProductionRate = GetProductionRate(MainViewModel.CurrentMinerType, MainViewModel.CurrentMinerSpeed, MainViewModel.CurrentResourceNodeType, MainViewModel.CurrentBeltSpeed)
            End If

            Return _ProductionRate.Value
        End Get
    End Property
    Public Function GetProductionRate(thisMinerType As MinerTypeEnum, thisMinerSpeed As MinerSpeedEnum, thisResourceRate As ResourceNodeTypeEnum, thisBeltSpeed As BeltSpeedEnum) As Double
        Dim myReturn As Double

        If (Not String.IsNullOrEmpty(Me.Name)) AndAlso (Me.Name.Equals("Water")) Then
            myReturn = 120 * (thisMinerSpeed / 100)
            If (myReturn > 300) Then myReturn = 300
        ElseIf (Not String.IsNullOrEmpty(Me.Name)) AndAlso (Me.Name.Equals("Crude Oil")) Then
            If (Me.GlobalSources IsNot Nothing) AndAlso (Me.GlobalSources.Any) Then
                myReturn = 120 * thisMinerType * (thisMinerSpeed / 100)
                If (myReturn > 300) Then myReturn = 300
            End If
        ElseIf (Me.GlobalSources IsNot Nothing) AndAlso (Me.GlobalSources.Any) Then
            myReturn = thisResourceRate * thisMinerType * (thisMinerSpeed / 100)
            If (myReturn > thisBeltSpeed) Then myReturn = thisBeltSpeed
        ElseIf (Me.ProductionPerMachine > 0) AndAlso (Me.ProductionPerMinute = 0) Then
            myReturn = Me.ProductionPerMachine * thisMinerType * (thisMinerSpeed / 100)
            If (myReturn > thisBeltSpeed) Then myReturn = thisBeltSpeed
        ElseIf (Me.ProductionPerMachine = 0) AndAlso (Me.ProductionPerMinute > 0) Then
            myReturn = Me.ProductionPerMinute
        ElseIf (Me.ItemsPerMinute > 0) Then
            myReturn = Me.ItemsPerMinute
        Else
            myReturn = 1
        End If

        Return myReturn
    End Function
    Public Overrides Function Equals(other As Object) As Boolean
        If (other Is Nothing) Then Return False
        If (Not other.GetType.Equals(Me.GetType)) Then Return False

        Return DirectCast(Me, IEquatable(Of Resource)).Equals(TryCast(other, Resource))
    End Function
    Public Overloads Function Equals(other As Resource) As Boolean Implements IEquatable(Of Resource).Equals
        If (other Is Nothing) Then Return False

        If (Not String.IsNullOrEmpty(Me.Name)) AndAlso (Not String.IsNullOrEmpty(other.Name)) AndAlso (Not String.IsNullOrEmpty(Me.Type)) AndAlso (Not String.IsNullOrEmpty(other.Type)) Then
            Return Me.Name.Equals(other.Name) AndAlso Me.Type.Equals(other.Type)
        ElseIf (Not String.IsNullOrEmpty(Me.Type)) AndAlso (Not String.IsNullOrEmpty(other.Type)) Then
            Return Me.Type.Equals(other.Type)
        Else
            Return False
        End If
    End Function
    Public Overrides Function GetHashCode() As Integer
        Return String.Format("{0}{1}", Me.Name, Me.Type).GetHashCode
    End Function
End Class
