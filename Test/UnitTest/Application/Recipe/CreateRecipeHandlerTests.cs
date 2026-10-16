using Catalog.Application.UseCase.Command.Recipe.CreateRecipe;
using Catalog.Domain.Factory.Recipe;
using Catalog.Domain.Model.Recipes;
using Catalog.Domain.Repository.Recipe;
using Catalog.Domain.ValueObjects;
using Microsoft.Extensions.Logging;
using Moq;
using Shared.Core;
using Xunit;

namespace Catalog.Tests.Application
{
    public class CreateRecipeHandlerTests
    {
        private readonly Mock<IRecipeRepository> _repoMock;
        private readonly Mock<IRecipeFactory> _factoryMock;
        private readonly Mock<IUnitOfWork> _unitOfWorkMock;
        private readonly Mock<ILogger<CreateRecipeHandler>> _loggerMock;
        private readonly CreateRecipeHandler _handler;

        public CreateRecipeHandlerTests()
        {
            _repoMock = new Mock<IRecipeRepository>();
            _factoryMock = new Mock<IRecipeFactory>();
            _unitOfWorkMock = new Mock<IUnitOfWork>();
            _loggerMock = new Mock<ILogger<CreateRecipeHandler>>();
            _handler = new CreateRecipeHandler(_repoMock.Object, _factoryMock.Object, _unitOfWorkMock.Object, _loggerMock.Object);
        }

        [Fact]
        public async Task Handle_ValidCommand_ReturnsSuccessAndVerifiesInteractions()
        {
            // Arrange
            var recipe = new Recipe(new RecipeName("Ensalada"), "Mezclar todos los ingredientes");
            _factoryMock.Setup(f => f.Create(It.IsAny<string>(), It.IsAny<string>())).Returns(recipe);
            var command = new CreateRecipeCommand { Name = "Ensalada", Instructions = "Mezclar todos los ingredientes" };

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.Equal(recipe.Id, result.Value);
            Assert.Equal("Recipe created successfully", result.Message);
            _factoryMock.Verify(f => f.Create("Ensalada", "Mezclar todos los ingredientes"), Times.Once);
            _repoMock.Verify(r => r.CreateAsync(It.Is<Recipe>(r2 => r2.Name.Name == "Ensalada")), Times.Once);
            _unitOfWorkMock.Verify(u => u.Commit(), Times.Once);
        }

        [Theory]
        [InlineData("", "")]
        [InlineData(null, "Mezclar")]
        [InlineData("Ensalada", "   ")]
        [InlineData("Ensalada", "")]
        public async Task Handle_DomainValidationFails_FactoryThrows_ReturnsFailureAndNoCommit(string? name, string? instructions)
        {
            // Arrange
            _factoryMock.Setup(f => f.Create(It.IsAny<string>(), It.IsAny<string>()))
                .Throws(new BussinessRuleValidationException("Recipe instructions cannot be empty"));
            var command = new CreateRecipeCommand { Name = name!, Instructions = instructions! };

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Equal("Recipe instructions cannot be empty", result.Message);
            _repoMock.Verify(r => r.CreateAsync(It.IsAny<Recipe>()), Times.Never);
            _unitOfWorkMock.Verify(u => u.Commit(), Times.Never);
            _loggerMock.Verify(
                x => x.Log(
                    LogLevel.Error,
                    It.IsAny<EventId>(),
                    It.IsAny<It.IsAnyType>(),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }
    }
}