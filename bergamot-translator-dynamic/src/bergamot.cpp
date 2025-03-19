#include "bergamot.h"
#include "translator/response.h"
#include "translator/response_options.h"
#include "translator/service.h"
#include "translator/utils.h"
#include "translator/translation_model.h"
#include <combaseapi.h>

using marian::bergamot::BlockingService;
using marian::bergamot::Response;
using marian::bergamot::ResponseOptions;
using marian::bergamot::TranslationModel;

// トランスレーターの状態を保持する構造体
struct BergamotTranslatorState
{
    // BlockingServiceとTranslationModelのインスタンスを保持
    std::unique_ptr<BlockingService> service;
    std::shared_ptr<TranslationModel> model;

    BergamotTranslatorState() {}
    ~BergamotTranslatorState() {}
};

extern "C"
{
    void *BergamotTranslator_Initialize(
        const char *modelPath,
        const char *shortlistPath,
        const char *vocabPath,
        const char *configYaml,
        const BergamotTranslationOptions *options)
    {
        // BlockingServiceの設定
        BlockingService::Config serviceConfig;
        serviceConfig.cacheSize = options ? options->cacheSize : 0;

        // トランスレーターの状態オブジェクト作成
        auto state = new BergamotTranslatorState();

        // BlockingServiceのインスタンス作成
        state->service = std::make_unique<BlockingService>(serviceConfig);

        // TranslationModelの設定
        TranslationModel::Config modelConfig;

        // // モデルファイルパスの設定
        // modelConfig.modelPath = modelPath;
        // modelConfig.shortlistPath = shortlistPath;
        // modelConfig.vocabPaths = {vocabPath};

        // // 設定ファイルが提供されていれば読み込む
        // if (configYaml && strlen(configYaml) > 0)
        // {
        //     modelConfig.gemm = "intgemm8"; // デフォルト設定

        //     // 追加設定を解析
        //     if (options)
        //     {
        //         modelConfig.beamSize = options->beamSize;
        //         modelConfig.normalize = options->normalizeScore ? 1.0f : 0.0f;
        //         modelConfig.maxLengthBreak = options->maxLengthBreak;
        //     }

        //     // configYamlがあれば適用
        //     modelConfig.yamlConfig = configYaml;
        // }

        // モデルのインスタンス作成
        state->model = std::make_shared<TranslationModel>(modelConfig);
        return static_cast<void *>(state);
    }

    char *BergamotTranslator_Translate(void *translator, const char *text)
    {
        if (!translator || !text)
        {
            return nullptr;
        }

        auto state = static_cast<BergamotTranslatorState *>(translator);

        // 翻訳オプションの設定
        ResponseOptions responseOptions;

        // 翻訳実行
        std::vector<std::string> sources = {text};
        std::vector<ResponseOptions> options = {responseOptions};

        std::vector<Response> responses = state->service->translateMultiple(
            state->model,
            std::move(sources),
            options);

        // 翻訳結果を取得
        if (!responses.empty())
        {
            auto translated = responses[0].target.text;
            size_t len = translated.size() + 1;
            char *result = (char *)CoTaskMemAlloc(len);
            memcpy(result, translated.c_str(), len);
            return result;
        }

        return nullptr;
    }

    void BergamotTranslator_Free(void *translator)
    {
        if (translator)
        {
            auto state = static_cast<BergamotTranslatorState *>(translator);
            delete state;
        }
    }

} // extern "C"
