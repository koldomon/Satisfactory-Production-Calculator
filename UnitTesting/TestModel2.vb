Imports System.Text
Imports System.Threading.Tasks.Dataflow
Imports Microsoft.VisualStudio.TestTools.UnitTesting
Imports Satisfactory.ObjectModel

<TestClass()> Public Class TestModel2
    Public Shared glbSourceFileFullName = IO.Path.Combine(My.Application.Info.DirectoryPath, "sources", "SatisfactoryRecipes2.xml")
    Public Shared glbDataStore As New DataStore
    '<ClassInitialize> Public Shared Sub InitClass(thisContext As TestContext)
    '    If (thisContext Is Nothing) Then Throw New ArgumentNullException(NameOf(thisContext))

    '    If (My.Settings.ActiveAlternateRecipes Is Nothing) Then My.Settings.ActiveAlternateRecipes = New Specialized.StringCollection

    '    Dim myStore As DataStore = LoadDataStore()

    '    If (myStore IsNot Nothing) AndAlso (myStore.Items.Any) Then glbDataStore.SetItems(myStore.Items)
    'End Sub

    Private Shared Sub LoadDefaultStore()
        If (My.Settings.ActiveAlternateRecipes Is Nothing) Then My.Settings.ActiveAlternateRecipes = New Specialized.StringCollection

        Dim myStore As DataStore = LoadDataStore()
        Assert.IsNotNull(myStore)
        Assert.IsTrue(myStore.Items.Any)

        glbDataStore.SetItems(myStore.Items)
        Assert.IsTrue(glbDataStore.Items.Any)
    End Sub

    Private Shared Function LoadDataStore() As DataStore
        Assert.IsTrue(IO.File.Exists(TestModel2.glbSourceFileFullName))

        Dim myStore = XMLHelper.ReadFromXML(Of DataStore)(glbSourceFileFullName)
        If (myStore IsNot Nothing) Then Return myStore

        Return Nothing
    End Function


    <TestMethod()> Public Sub TestLoadRecipesFromGamepedia()
        Dim myStore As New DataStore
        myStore.LoadRecipesFromGamepedia()

        Assert.IsTrue(myStore.Items.Any)

        XMLHelper.WriteToXML(Of DataStore)(myStore, TestModel2.glbSourceFileFullName)

        GC.Collect()
        GC.WaitForFullGCComplete()
    End Sub

    <TestMethod()> Public Sub TestCalculateRecipeCosts()
        Dim myCTL = New ConsoleTraceListener(False)
        Trace.Listeners.Add(myCTL)

        Assert.IsTrue(IO.File.Exists(TestModel2.glbSourceFileFullName))

        Dim myStore = XMLHelper.ReadFromXML(Of DataStore)(glbSourceFileFullName)
        Assert.IsNotNull(myStore)
        Assert.IsTrue(myStore.Items.Any)

        myStore.CalculateRecipeCosts()

        Trace.Listeners.Remove(myCTL)
    End Sub


    '<TestMethod()> Public Sub TestGetRecipeCosts()
    '    Dim myCTL = New ConsoleTraceListener(False)
    '    Trace.Listeners.Add(myCTL)

    '    LoadDefaultStore()

    '    Dim myResource = glbDataStore.Items.OfType(Of WorldItem).FirstOrDefault(Function(x) x.ItemType = "Limestone")
    '    Trace.WriteLine(String.Format("{0};{1}", myResource.ItemType, myResource.Costs))

    '    Dim myRecipe As Recipe = Nothing
    '    Dim myCosts As Double = 0

    '    myRecipe = glbDataStore.Items.OfType(Of Recipe).FirstOrDefault(Function(x) x.Name = "Concrete")
    '    Trace.WriteLine(String.Format("{0}", myRecipe.Name))
    '    myCosts = myRecipe.Costs
    '    Trace.WriteLine(String.Format("{0};{1}", myRecipe.Name, myCosts))

    '    myRecipe = glbDataStore.Items.OfType(Of Recipe).FirstOrDefault(Function(x) x.Name = "Wet Concrete")
    '    Trace.WriteLine(String.Format("{0}", myRecipe.Name))
    '    myCosts = myRecipe.Costs
    '    Trace.WriteLine(String.Format("{0};{1}", myRecipe.Name, myCosts))

    '    Trace.Listeners.Remove(myCTL)
    'End Sub


    <DataTestMethod, DynamicData(NameOf(GetAllRecipes), DynamicDataSourceType.Method)> Public Sub TestGetRecipeCosts(thisRecipe As Recipe)
        Dim myCTL = New ConsoleTraceListener(False)
        Trace.Listeners.Add(myCTL)

        Assert.IsNotNull(glbDataStore)
        Assert.IsTrue(glbDataStore.Items.Any)

        Dim myCosts = thisRecipe.Costs
        Trace.WriteLine(String.Format("{0};{1}", thisRecipe.Name, myCosts))

        Trace.Listeners.Remove(myCTL)
    End Sub

    '<DataTestMethod, DynamicData(NameOf(GetAllRecipes), DynamicDataSourceType.Method)>
    'Public Sub TestAllProductions(thisRecipe As ResourceView)
    '    Dim myProdView = glbViewModel.AddProduction(thisRecipe, 10)
    '    Assert.IsNotNull(myProdView.Productions)
    '    Assert.IsNotNull(myProdView.AdditionalItems)
    'End Sub


    Private Shared Iterator Function GetAllRecipes() As IEnumerable(Of Object())
        LoadDefaultStore()

        For Each myRecipe In glbDataStore.Items.OfType(Of Recipe)
            Yield New Object() {myRecipe}
        Next
    End Function

End Class