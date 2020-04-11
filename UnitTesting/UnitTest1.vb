Imports System.Text
Imports Microsoft.VisualStudio.TestTools.UnitTesting

<TestClass()> Public Class UnitTest1

    <TestMethod()> Public Sub TestMethod1()
        Dim myVal As Boolean = True
        Assert.IsTrue(myVal)
    End Sub

End Class