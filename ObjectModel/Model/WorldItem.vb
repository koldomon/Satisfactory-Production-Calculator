
Imports System.Xml.Serialization

<DebuggerDisplay("WoI: [{ItemType}]")> Public Class WorldItem
    Inherits Item
    Implements IEquatable(Of WorldItem)

    Public Const cMaxPipeThrouput As Integer = 300

    Private _ImpureCount As Double
    Private _NormalCount As Double
    Private _PureCount As Double
    Private _BaseValue As BaseProductionEnum = BaseProductionEnum.Default
    Private _TotalItems As Double?
    Private _Costs As Double?

    <XmlAttribute> Public Property ImpureCount As Double
        Get
            Return _ImpureCount
        End Get
        Set
            _ImpureCount = Value
        End Set
    End Property

    <XmlAttribute> Public Property NormalCount As Double
        Get
            Return _NormalCount
        End Get
        Set
            _NormalCount = Value
        End Set
    End Property

    <XmlAttribute> Public Property PureCount As Double
        Get
            Return _PureCount
        End Get
        Set
            _PureCount = Value
        End Set
    End Property

    <XmlAttribute> Public Property BaseValue As BaseProductionEnum
        Get
            Return _BaseValue
        End Get
        Set
            _BaseValue = Value
        End Set
    End Property

    <XmlIgnore> Public ReadOnly Property TotalItems As Double
        Get
            If (Not _TotalItems.HasValue) Then _TotalItems = GetTotalItems(MinerTypeEnum.MK1, MinerSpeedEnum.Speed100, BeltSpeedEnum.MK6)

            Return _TotalItems.Value
        End Get
    End Property

    <XmlIgnore> Public Overrides ReadOnly Property Costs As Double
        Get
            If (Not _Costs.HasValue) Then _Costs = 1 / TotalItems

            Return _Costs.Value
        End Get
    End Property

    Public Function GetTotalItems(thisMinerType As MinerTypeEnum, thisMinerSpeed As MinerSpeedEnum, thisBeltSpeed As BeltSpeedEnum) As Double
        Dim myReturn As Double = 0

        myReturn += Me.ImpureCount * GetProductionMultiplier(ResourceNodeTypeEnum.Impure, thisMinerType, thisMinerSpeed, thisBeltSpeed)
        myReturn += Me.NormalCount * GetProductionMultiplier(ResourceNodeTypeEnum.Normal, thisMinerType, thisMinerSpeed, thisBeltSpeed)
        myReturn += Me.PureCount * GetProductionMultiplier(ResourceNodeTypeEnum.Pure, thisMinerType, thisMinerSpeed, thisBeltSpeed)

        Return myReturn
    End Function

    Private Function GetProductionMultiplier(thisResourceNodeType As ResourceNodeTypeEnum, thisMinerType As MinerTypeEnum, thisMinerSpeed As MinerSpeedEnum, thisBeltSpeed As BeltSpeedEnum) As Double
        Dim myProductionMultiplier As Double = 0
        Select Case thisResourceNodeType
            Case ResourceNodeTypeEnum.Impure
                myProductionMultiplier = (Me.BaseValue * thisMinerType * (thisMinerSpeed / 100)) / 2
            Case ResourceNodeTypeEnum.Normal
                myProductionMultiplier = (Me.BaseValue * thisMinerType * (thisMinerSpeed / 100))
            Case ResourceNodeTypeEnum.Pure
                myProductionMultiplier = (Me.BaseValue * thisMinerType * (thisMinerSpeed / 100)) * 2
        End Select

        If (Me.BaseValue = BaseProductionEnum.Default) AndAlso (myProductionMultiplier > thisBeltSpeed) Then
            myProductionMultiplier = thisBeltSpeed
        ElseIf ((Me.BaseValue = BaseProductionEnum.Water) OrElse (Me.BaseValue = BaseProductionEnum.CrudeOil)) AndAlso (myProductionMultiplier > cMaxPipeThrouput) Then
            myProductionMultiplier = cMaxPipeThrouput
        End If

        Return myProductionMultiplier
    End Function

    Public Overrides Function Equals(other As Object) As Boolean
        If (other Is Nothing) Then Return False
        If (Not other.GetType.Equals(Me.GetType)) Then Return False

        Return DirectCast(Me, IEquatable(Of WorldItem)).Equals(TryCast(other, WorldItem))
    End Function
    Public Overloads Function Equals(other As WorldItem) As Boolean Implements IEquatable(Of WorldItem).Equals
        If (other Is Nothing) Then Return False

        If (Not String.IsNullOrEmpty(Me.ItemType)) AndAlso (Not String.IsNullOrEmpty(other.ItemType)) Then
            Return Me.ItemType.Equals(other.ItemType)
        Else
            Return False
        End If
    End Function

    Public Overrides Function GetHashCode() As Integer
        Return String.Format("{0}", Me.ItemType).GetHashCode
    End Function

End Class
