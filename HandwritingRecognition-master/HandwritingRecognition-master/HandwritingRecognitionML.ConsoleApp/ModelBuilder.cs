// This file was auto-generated by ML.NET Model Builder. 

using Microsoft.ML;
using Microsoft.ML.Data;
using HandwritingRecognitionML.Model;

namespace HandwritingRecognitionML.ConsoleApp;

public static class ModelBuilder
{
    private const string TrainDataFilepath = "../../../../Data/optdigits-train.csv";
    private const string ModelFilepath = "../../../../HandwritingRecognition/MLModel.zip";

    // Create MLContext to be shared across the model creation workflow objects 
    // Set a random seed for repeatable/deterministic results across multiple trainings.
    private static readonly MLContext MlContext = new(seed: 1);

    public static void CreateModel()
    {
        // Load Data
        var trainingDataView = MlContext.Data.LoadFromTextFile<ModelInput>(
            path: GetAbsolutePath(TrainDataFilepath),
            hasHeader: true,
            separatorChar: ',',
            allowQuoting: true,
            allowSparse: false);

        // Build training pipeline
        var trainingPipeline = BuildTrainingPipeline(MlContext);

        // Evaluate quality of Model
        Evaluate(MlContext, trainingDataView, trainingPipeline);

        // Train Model
        var mlModel = TrainModel(MlContext, trainingDataView, trainingPipeline);

        // Save model
        SaveModel(MlContext, mlModel, ModelFilepath, trainingDataView.Schema);
    }

    public static IEstimator<ITransformer> BuildTrainingPipeline(MLContext mlContext)
    {
        // Data process configuration with pipeline data transformations 
        var dataProcessPipeline = mlContext.Transforms.Conversion.MapValueToKey("Number", "Number")
            .Append(mlContext.Transforms.Concatenate("Features", "PixelValues"))
            .AppendCacheCheckpoint(mlContext);


        // Set the training algorithm 
        var trainer = mlContext.MulticlassClassification.Trainers.LightGbm(labelColumnName: "Number", featureColumnName: "Features")
            .Append(mlContext.Transforms.Conversion.MapKeyToValue("PredictedLabel", "PredictedLabel"));
        var trainingPipeline = dataProcessPipeline.Append(trainer);

        return trainingPipeline;
    }

    public static ITransformer TrainModel(MLContext mlContext, IDataView trainingDataView, IEstimator<ITransformer> trainingPipeline)
    {
        Console.WriteLine("=============== Training  model ===============");

        var model = trainingPipeline.Fit(trainingDataView);

        Console.WriteLine("=============== End of training process ===============");
        return model;
    }

    private static void Evaluate(MLContext mlContext, IDataView trainingDataView, IEstimator<ITransformer> trainingPipeline)
    {
        // Cross-Validate with single dataset (since we don't have two datasets, one for training and for evaluate)
        // in order to evaluate and get the model's accuracy metrics
        Console.WriteLine("=============== Cross-validating to get model's accuracy metrics ===============");
        var crossValidationResults = mlContext.MulticlassClassification.CrossValidate(trainingDataView, trainingPipeline, numberOfFolds: 5, labelColumnName: "Number");
        PrintMulticlassClassificationFoldsAverageMetrics(crossValidationResults);
    }

    private static void SaveModel(MLContext mlContext, ITransformer mlModel, string modelRelativePath, DataViewSchema modelInputSchema)
    {
        // Save/persist the trained model to a .ZIP file
        Console.WriteLine("=============== Saving the model  ===============");
        mlContext.Model.Save(mlModel, modelInputSchema, GetAbsolutePath(modelRelativePath));
        Console.WriteLine("The model is saved to {0}", GetAbsolutePath(modelRelativePath));
    }

    public static string GetAbsolutePath(string relativePath)
    {
        var dataRoot = new FileInfo(typeof(Program).Assembly.Location);
        var assemblyFolderPath = dataRoot.Directory!.FullName;

        var fullPath = Path.Combine(assemblyFolderPath, relativePath);

        return fullPath;
    }

    public static void PrintMulticlassClassificationMetrics(MulticlassClassificationMetrics metrics)
    {
        Console.WriteLine("************************************************************");
        Console.WriteLine("*    Metrics for multi-class classification model   ");
        Console.WriteLine("*-----------------------------------------------------------");
        Console.WriteLine($"    MacroAccuracy = {metrics.MacroAccuracy:0.####}, a value between 0 and 1, the closer to 1, the better");
        Console.WriteLine($"    MicroAccuracy = {metrics.MicroAccuracy:0.####}, a value between 0 and 1, the closer to 1, the better");
        Console.WriteLine($"    LogLoss = {metrics.LogLoss:0.####}, the closer to 0, the better");
        for (var i = 0; i < metrics.PerClassLogLoss.Count; i++)
        {
            Console.WriteLine($"    LogLoss for class {i + 1} = {metrics.PerClassLogLoss[i]:0.####}, the closer to 0, the better");
        }
        Console.WriteLine("************************************************************");
    }

    public static void PrintMulticlassClassificationFoldsAverageMetrics(IEnumerable<TrainCatalogBase.CrossValidationResult<MulticlassClassificationMetrics>> crossValResults)
    {
        var metricsInMultipleFolds = crossValResults.Select(r => r.Metrics).ToList();

        var microAccuracyValues = metricsInMultipleFolds.Select(m => m.MicroAccuracy).ToList();
        var microAccuracyAverage = microAccuracyValues.Average();
        var microAccuraciesStdDeviation = CalculateStandardDeviation(microAccuracyValues);
        var microAccuraciesConfidenceInterval95 = CalculateConfidenceInterval95(microAccuracyValues);

        var macroAccuracyValues = metricsInMultipleFolds.Select(m => m.MacroAccuracy).ToList();
        var macroAccuracyAverage = macroAccuracyValues.Average();
        var macroAccuraciesStdDeviation = CalculateStandardDeviation(macroAccuracyValues);
        var macroAccuraciesConfidenceInterval95 = CalculateConfidenceInterval95(macroAccuracyValues);

        var logLossValues = metricsInMultipleFolds.Select(m => m.LogLoss).ToList();
        var logLossAverage = logLossValues.Average();
        var logLossStdDeviation = CalculateStandardDeviation(logLossValues);
        var logLossConfidenceInterval95 = CalculateConfidenceInterval95(logLossValues);

        var logLossReductionValues = metricsInMultipleFolds.Select(m => m.LogLossReduction).ToList();
        var logLossReductionAverage = logLossReductionValues.Average();
        var logLossReductionStdDeviation = CalculateStandardDeviation(logLossReductionValues);
        var logLossReductionConfidenceInterval95 = CalculateConfidenceInterval95(logLossReductionValues);

        Console.WriteLine("*************************************************************************************************************");
        Console.WriteLine("*       Metrics for Multi-class Classification model      ");
        Console.WriteLine("*------------------------------------------------------------------------------------------------------------");
        Console.WriteLine($"*       Average MicroAccuracy:    {microAccuracyAverage:0.###}  - Standard deviation: ({microAccuraciesStdDeviation:#.###})  - Confidence Interval 95%: ({microAccuraciesConfidenceInterval95:#.###})");
        Console.WriteLine($"*       Average MacroAccuracy:    {macroAccuracyAverage:0.###}  - Standard deviation: ({macroAccuraciesStdDeviation:#.###})  - Confidence Interval 95%: ({macroAccuraciesConfidenceInterval95:#.###})");
        Console.WriteLine($"*       Average LogLoss:          {logLossAverage:#.###}  - Standard deviation: ({logLossStdDeviation:#.###})  - Confidence Interval 95%: ({logLossConfidenceInterval95:#.###})");
        Console.WriteLine($"*       Average LogLossReduction: {logLossReductionAverage:#.###}  - Standard deviation: ({logLossReductionStdDeviation:#.###})  - Confidence Interval 95%: ({logLossReductionConfidenceInterval95:#.###})");
        Console.WriteLine($"*************************************************************************************************************");

    }

    public static double CalculateStandardDeviation(IReadOnlyList<double> values)
    {
        var average = values.Average();
        var sumOfSquaresOfDifferences = values.Select(val => (val - average) * (val - average)).Sum();
        var standardDeviation = Math.Sqrt(sumOfSquaresOfDifferences / (values.Count - 1));
        return standardDeviation;
    }

    public static double CalculateConfidenceInterval95(IReadOnlyList<double> values)
    {
        var confidenceInterval95 = 1.96 * CalculateStandardDeviation(values) / Math.Sqrt(values.Count - 1);
        return confidenceInterval95;
    }
}