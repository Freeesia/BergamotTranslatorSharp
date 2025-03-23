#include "bergamot.h"
#include "translator/response.h"
#include "translator/response_options.h"
#include "translator/service.h"
#include "translator/translation_model.h"
#ifdef _WIN32
#include <combaseapi.h>
#else
#include <gperftools/tcmalloc.h>
#endif

using marian::bergamot::BlockingService;
using marian::bergamot::parseOptionsFromFilePath;
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
    void *translator_initialize(const char *configPath)
    {
        // BlockingServiceの設定
        BlockingService::Config serviceConfig;

        // トランスレーターの状態オブジェクト作成
        auto state = new BergamotTranslatorState();

        // BlockingServiceのインスタンス作成
        state->service = std::make_unique<BlockingService>(serviceConfig);

        auto options = parseOptionsFromFilePath(configPath);
        // モデルのインスタンス作成
        state->model = std::make_shared<TranslationModel>(options);
        return static_cast<void *>(state);
    }

    char *translator_translate(void *translator, const char *text, bool html)
    {
        if (!translator || !text)
        {
            return nullptr;
        }

        auto state = static_cast<BergamotTranslatorState *>(translator);

        // 翻訳オプションの設定
        ResponseOptions responseOptions;
        responseOptions.HTML = html;  // HTMLパラメータを設定

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
#ifdef _WIN32
            char *result = (char *)CoTaskMemAlloc(len);
#else
            // tcmallocを直接使用してメモリ確保
            char *result = (char *)malloc(len);
#endif
            memcpy(result, translated.c_str(), len);
            return result;
        }

        return nullptr;
    }

    void translator_free(void *translator)
    {
        if (translator)
        {
            auto state = static_cast<BergamotTranslatorState *>(translator);
            delete state;
        }
    }

} // extern "C"
