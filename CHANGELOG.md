# Changelog

## [1.1.0](https://github.com/wondertalik/azure-container-apps/compare/v1.0.0...v1.1.0) (2025-07-26)


### Features

* add bicep configuration for httpApi ([3f81178](https://github.com/wondertalik/azure-container-apps/commit/3f8117830253ae61a49012e4e369c77d2ef04f0d))
* add docker configuration for httpapi ([b97e5ee](https://github.com/wondertalik/azure-container-apps/commit/b97e5eed5db056c9cd3eb036f90c88fa57bc3bd6))
* add filters for tracing ([d34ceee](https://github.com/wondertalik/azure-container-apps/commit/d34ceeedcc1fe7dc23ef4368d8f0921ce544ce91))
* add health check for httpapi ([53d5987](https://github.com/wondertalik/azure-container-apps/commit/53d5987bce21e5d723aa53e92f741d15e765f50d))
* add opentelemetry ([69c3785](https://github.com/wondertalik/azure-container-apps/commit/69c37850a16d077ad887199b33b899d9a17eaf35))
* add release-please ([59cfbd1](https://github.com/wondertalik/azure-container-apps/commit/59cfbd18ce21c1c5020b8c225375d10f77a03e98))
* add resourcesCpu and resourcesMemory ([975ba4a](https://github.com/wondertalik/azure-container-apps/commit/975ba4af40abcf0b087e36bc64e6eca99bc4706e))
* add scale parameters ([b5b1239](https://github.com/wondertalik/azure-container-apps/commit/b5b123970729efe8b4aa64a303af4d194e94344d))
* deploy azure function separately ([87fb84c](https://github.com/wondertalik/azure-container-apps/commit/87fb84c42e911e498e0ee2a2af297798bdb161ab))
* enable aspire dashboard and send otel data to it ([ed45a6c](https://github.com/wondertalik/azure-container-apps/commit/ed45a6c93b405e6c2c1fa9dfd5ca91bcd6be7446))
* enable autoConfigureDataProtection for azure container app ([c0368fc](https://github.com/wondertalik/azure-container-apps/commit/c0368fc60d404753b83b6e83044b585467e65417))
* enable azure monitor ([4d4ea6b](https://github.com/wondertalik/azure-container-apps/commit/4d4ea6bf821f86cc4f363e9ef5acf656c067449f))
* remove azure noising traces ([b676591](https://github.com/wondertalik/azure-container-apps/commit/b67659153e83dd29dda5cde097f4dc5cb8eef96d))
* update examples, add otel-collector, sentry ([#1](https://github.com/wondertalik/azure-container-apps/issues/1)) ([2c8259e](https://github.com/wondertalik/azure-container-apps/commit/2c8259e9331c230e710c7d4a8150cc5e43fef018))
* use secrets for FunctionApp1 ([657106a](https://github.com/wondertalik/azure-container-apps/commit/657106aa916dce868cc77d90dd8096b3c10a2804))
* Use system managed identity instead of AzureWebJobsStorage to connect a function app to a storage account ([5615d53](https://github.com/wondertalik/azure-container-apps/commit/5615d538864f0042bbf03b4cd70c012cafa92b81))


### Bug Fixes

* fix dockerfiles and docker-compose configuration ([#2](https://github.com/wondertalik/azure-container-apps/issues/2)) ([6b83778](https://github.com/wondertalik/azure-container-apps/commit/6b83778ab724f4b48e2d4a916442f509907a7ce0))
* set the correct environment name when deploy azure function ([c9e05c3](https://github.com/wondertalik/azure-container-apps/commit/c9e05c31c0446e2305e9bbd2b7e1a458c0b3013b))


### Code Refactoring

* add default images ([b63de49](https://github.com/wondertalik/azure-container-apps/commit/b63de49ccba7a685028db12dbaf2e830cf9eaf50))
* enable deploying functionApp1 ([026ce5f](https://github.com/wondertalik/azure-container-apps/commit/026ce5fc42bb705b8bbea539f492c644e6fa0cc9))
* use OTEL_EXPORTER_OTLP_HEADERS from .env ([85872e0](https://github.com/wondertalik/azure-container-apps/commit/85872e0fe415ad9b28b4e59a96e89d0600cacfd7))


### Continuous Integration

* setup release please ([#5](https://github.com/wondertalik/azure-container-apps/issues/5)) ([0e97e58](https://github.com/wondertalik/azure-container-apps/commit/0e97e5846833f2b9425140d5f2fe3c12ac70c877))
* update bicep api versions ([#3](https://github.com/wondertalik/azure-container-apps/issues/3)) ([134ff39](https://github.com/wondertalik/azure-container-apps/commit/134ff39cf9149decedb0de95a8c5d75eccd6aeae))


### Documentation

* improve readme ([4762c81](https://github.com/wondertalik/azure-container-apps/commit/4762c811d6edb7c0841f7efd1294d7c82348da6c))
