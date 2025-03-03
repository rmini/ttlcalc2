
namespace TtlCalcSim.Tests;

public class InputOutputTests
{
    [Fact]
    public void Read_WithValidAddress_ReturnsExpectedNybble()
    {
        // Arrange
        var io = new InputOutput();
        io.AddInputHandler(0x00, 0x0F, addr => 0xA);

        // Act
        var result = io.Read(0x05);

        // Assert
        Assert.Equal((byte)0xA, (byte)result);
    }

    [Fact]
    public void Read_WithInvalidAddress_ThrowsInvalidOperationException()
    {
        // Arrange
        var io = new InputOutput();

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => io.Read(0x05));
    }

    [Fact]
    public void Read_WithOutOfOrderHandlers_ReturnsExpectedResults()
    {
        // Arrange
        var io = new InputOutput();
        io.AddInputHandler(0xF0, 0xFF, addr => 0x5);
        io.AddInputHandler(0x00, 0x0F, addr => 0xA);

        // Act
        var result1 = io.Read(0x05);
        var result2 = io.Read(0xF5);

        // Assert
        Assert.Equal((byte)0xA, (byte)result1);
        Assert.Equal((byte)0x5, (byte)result2);
    }

    [Fact]
    public void Write_WithValidAddress_CallsHandler()
    {
        // Arrange
        var io = new InputOutput();
        bool handlerCalled = false;
        io.AddOutputHandler(0x00, 0x0F, (addr, d) => handlerCalled = true);

        // Act
        io.Write(0x05, 0xA);

        // Assert
        Assert.True(handlerCalled);
    }

    [Fact]
    public void AddInputHandler_WithOverlappingRange_ThrowsInvalidOperationException()
    {
        // Arrange
        var io = new InputOutput();

        Nybble HandlerFunc(byte addr) => 0xA;

        io.AddInputHandler(0x00, 0x0F, HandlerFunc);

        io.Read(0); // Make code coverage happy

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => io.AddInputHandler(0x05, 0x10, HandlerFunc));
    }

    [Fact]
    public void RemoveInputHandler_WithValidIndex_RemovesHandler()
    {
        // Arrange
        var io = new InputOutput();
        io.AddInputHandler(0x00, 0x0F, addr => 0xA);

        io.Read(0); // Make code coverage happy
        // Act
        io.RemoveInputHandler(0);

        // Assert
        Assert.Empty(io.InputHandlers);
    }

    [Fact]
    public void RemoveOutputHandler_WithValidIndex_RemovesHandler()
    {
        // Arrange
        var io = new InputOutput();
        io.AddOutputHandler(0x00, 0x0F, (addr, d) => { });

        io.Write(0, 0); // Make code coverage happy
        // Act
        io.RemoveOutputHandler(0);

        // Assert
        Assert.Empty(io.OutputHandlers);
    }

    [Fact]
    public void RemoveInputHandler_WithInvalidIndex_ThrowsArgumentOutOfRangeException()
    {
        // Arrange
        var io = new InputOutput();
        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => io.RemoveInputHandler(0));
    }

    [Fact]
    public void RemoveOutputHandler_WithInvalidIndex_ThrowsArgumentOutOfRangeException()
    {
        // Arrange
        var io = new InputOutput();
        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => io.RemoveOutputHandler(0));
    }

    [Fact]
    public void RemoveInputHandler_WithNegativeIndex_ThrowsArgumentOutOfRangeException()
    {
        // Arrange
        var io = new InputOutput();
        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => io.RemoveInputHandler(-1));
    }

    [Fact]
    public void RemoveOutputHandler_WithNegativeIndex_ThrowsArgumentOutOfRangeException()
    {
        // Arrange
        var io = new InputOutput();
        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => io.RemoveOutputHandler(-1));
    }

    [Fact]
    public void Read_WithNoHandlers_CallsDefaultHandler()
    {
        // Arrange
        var io = new InputOutput();
        io.DefaultInputHandler = addr => 0xB;

        // Act
        var result = io.Read(0x05);

        // Assert
        Assert.Equal((byte)0xB, (byte)result);
    }

    [Fact]
    public void Write_WithNoHandlers_DoesNotThrow()
    {
        // Arrange
        var io = new InputOutput();
        // Act & Assert
        io.Write(0x05, 0xA);
    }

    [Fact]
    public void Read_WithMultipleHandlers_CallsCorrectHandler()
    {
        // Arrange
        var io = new InputOutput();
        io.AddInputHandler(0x00, 0x0F, addr => 0xA);
        io.AddInputHandler(0x10, 0x1F, addr => 0xB);

        // Act
        var result1 = io.Read(0x05);
        var result2 = io.Read(0x15);

        // Assert
        Assert.Equal((byte)0xA, (byte)result1);
        Assert.Equal((byte)0xB, (byte)result2);
    }

    [Fact]
    public void Write_WithMultipleHandlers_CallsCorrectHandler()
    {
        // Arrange
        var io = new InputOutput();
        bool handler1Called = false;
        bool handler2Called = false;
        io.AddOutputHandler(0x00, 0x0F, (addr, d) => handler1Called = true);
        io.AddOutputHandler(0x10, 0x1F, (addr, d) => handler2Called = true);
        // Act
        io.Write(0x05, 0xA);
        io.Write(0x15, 0xB);
        // Assert
        Assert.True(handler1Called);
        Assert.True(handler2Called);
    }

    [Fact]
    public void Write_WithMultipleHandlers_CallsAllValidHandlers()
    {
        // Arrange
        var io = new InputOutput();
        bool handler1Called = false;
        bool handler2Called = false;
        io.AddOutputHandler(0x00, 0x0F, (addr, d) => handler1Called = true);
        io.AddOutputHandler(0x00, 0x0F, (addr, d) => handler2Called = true);

        // Act
        io.Write(0x05, 0xA);

        // Assert
        Assert.True(handler1Called);
        Assert.True(handler2Called);
    }

    [Fact]
    public void Write_WithMultipleHandlers_CallsAllValidHandlersInOrder()
    {
        // Arrange
        var io = new InputOutput();
        int order = 0;
        int handler1Called = 0;
        int handler2Called = 0;
        io.AddOutputHandler(0x00, 0x0F, (addr, d) => handler1Called = ++order);
        io.AddOutputHandler(0x00, 0x0F, (addr, d) => handler2Called = ++order);
        // Act
        io.Write(0x05, 0xA);
        // Assert
        Assert.Equal(1, handler1Called);
        Assert.Equal(2, handler2Called);
    }
}
