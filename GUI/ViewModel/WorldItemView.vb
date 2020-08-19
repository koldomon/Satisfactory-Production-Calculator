Imports System.Collections.ObjectModel
Imports Satisfactory.ObjectModel


Public Class WorldItemView
    Inherits ItemView
#Region ""
#End Region
End Class

Partial Class WorldItemView
#Region "Base Properties"
    Public ReadOnly Property TotalItems As Double
        Get
            Return DirectCast(baseObj, WorldItem).GetTotalItems(baseStoreView.MinerType, baseStoreView.MinerSpeed, baseStoreView.BeltSpeed)
        End Get
    End Property

    Public ReadOnly Property Costs As Double
        Get
            Return baseObj.Costs
        End Get
    End Property

#End Region
End Class

Partial Class WorldItemView
#Region ""
#End Region
End Class

