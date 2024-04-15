namespace BitBlazorUI {
    interface BitChartConfiguration extends Chart.ChartConfiguration {
        canvasId: string;
    }

    interface DotNetObjectReference {
        invokeMethod(methodName: string, ...args: any[]): any;
        invokeMethodAsync(methodName: string, ...args: any[]): Promise<any>;
    }

    // maybe the interop could be improved somehow by using generic similar to how it's done in C#
    // but since we don't need typesafety in JavaScript and can't rely on it anyway (just like we
    // have to invoke the C# delegate dynamically) we're not using generics.
    export interface IMethodHandler {
        methodName: string;
    }

    interface DelegateHandler extends IMethodHandler {
        handlerReference: DotNetObjectReference;
        returnsValue: boolean;
        ignoredIndices: number[];
    }

    export class BitChart {
        private static _initPromise?: Promise<unknown>;
        private static _bitCharts = new Map<string, Chart>();

        public static getChartJs(canvasId: string) {
            if (!BitChart._bitCharts.has(canvasId)) return null;

            return BitChart._bitCharts.get(canvasId)!;
        }

        public static async initChartJs(scripts: string[]) {
            if (BitChart._initPromise) {
                await BitChart._initPromise;
            }

            const allScripts = Array.from(document.scripts).map(s => s.src);
            const notAppenedScripts = scripts.filter(s => !allScripts.find(as => as.endsWith(s)));

            if (notAppenedScripts.length == 0) return Promise.resolve();

            const promise = new Promise(async (resolve: any, reject: any) => {
                try {
                    for (let url of notAppenedScripts) await addScript(url);
                    resolve();
                } catch (e: any) {
                    reject(e);
                }
            });
            BitChart._initPromise = promise;
            return promise;

            async function addScript(url: string) {
                return new Promise((res, rej) => {
                    const script = document.createElement('script');
                    script.src = url;
                    script.onload = res;
                    script.onerror = rej;
                    document.body.appendChild(script);
                })
            }
        }

        public static removeChart(canvasId: string) {
            if (!BitChart._bitCharts.has(canvasId)) return;

            var chart = BitChart._bitCharts.get(canvasId)!;
            chart.destroy();
            chart.config = {};
            BitChart._bitCharts.delete(canvasId);
        }

        public static setupChart(config: BitChartConfiguration): boolean {
            if (BitChart._bitCharts.has(config.canvasId)) {
                return BitChart.updateChart(config);
            } else {
                BitChart.wireUpCallbacks(config);

                let chart = new Chart(config.canvasId, config);
                BitChart._bitCharts.set(config.canvasId, chart);

                return true;
            }
        }

        public static updateChart(config: BitChartConfiguration): boolean {
            if (!BitChart._bitCharts.has(config.canvasId))
                throw `Could not find a chart with the given id. ${config.canvasId}`;

            let myChart = BitChart._bitCharts.get(config.canvasId);

            if (!myChart) return false;

            // Update datasets. This breaks the data-array-references; more in the function.
            BitChart.mergeDatasets(myChart.config.data!.datasets!, config.data!.datasets!);
            // Update labels while keeping array references intact.
            BitChart.mergeLabels(myChart.config.data!, config.data!);
            // Currently we only merge the datasets and the labels of the data subconfig but that
            // could be expanded in a similar fashion as the dataset's data (if there's a use-case).

            BitChart.wireUpCallbacks(config);

            // This will add new options and update existing ones. Nothing is deleted.
            // Calling extend instead of merge avoids the unnecessary deep copy as
            // config.options is a brand new object (deserialized by blazor).
            Chart.helpers.extend(myChart.config.options, config.options);

            myChart.update();
            return true;
        }

        private static mergeDatasets(oldDatasets: Array<Chart.ChartDataSets>, newDatasets: Array<Chart.ChartDataSets>) {
            // iterate backwards so we can remove datasets as we go
            for (let i = oldDatasets.length - 1; i >= 0; i--) {
                let sameDatasetInNewConfig = newDatasets.find(newD => newD.id === oldDatasets[i].id);
                if (sameDatasetInNewConfig === undefined) {
                    // Remove dataset if it's not in the new config
                    oldDatasets.splice(i, 1);
                } else {
                    // This comment below would be the 'correct' way of updating the data while retaining the same reference.
                    // However, there's quite a big issue with BitChart. Chart.js actually listenes for modifications on
                    // the data array and will decide on the update-animation by looking at the latest modifications.
                    // Since this would clear the whole array and then add all the new data, Chart.js thinks every data
                    // point is fresh and plays the same animation it plays when initially creating the chart.
                    // To prevent Chart.js from doing that, we replace the reference which doesn't record any modifications.

                    //oldDatasets[i].data.length = 0; // Remove old data
                    //for (let j = 0; j < sameDatasetInNewConfig.data.length; j++) {
                    //    // Add current data. Of course it won't be a number _and_ a ChartPoint but I don't how else to make ts happy
                    //    oldDatasets[i].data.push(<number & Chart.ChartPoint>sameDatasetInNewConfig.data[j]);
                    //}
                    //delete sameDatasetInNewConfig.data; // Remove the array from the new dataset so it doesn't get copied in the next line

                    // Merge everything, including the data-array reference.
                    // As with the labels, deep copying(with helper.merge) is simply a waste here.
                    Chart.helpers.extend(oldDatasets[i], sameDatasetInNewConfig);
                }
            }

            let currentIds = oldDatasets.map(dataset => dataset.id);
            newDatasets.filter(newDataset => !currentIds.includes(newDataset.id))
                .forEach(newDataset => oldDatasets.push(newDataset));

            // Currently the order isn't respected so simply reordering the datasets and calling update
            // won't do anything. You'd have to remove and readd them. Maybe this could be implemented later.
        }

        private static mergeLabels(oldChartData: Chart.ChartData, newChartData: Chart.ChartData): void {
            const innerFunc = (oldLabels: Array<string | string[] | number | number[] | Date | Date[]>,
                newLabels: Array<string | string[] | number | number[] | Date | Date[]>) => {
                if (newLabels == null || newLabels.length === 0) {
                    if (oldLabels) {
                        oldLabels.length = 0;
                    }

                    return oldLabels;
                }

                if (oldLabels == null) {
                    return newLabels;
                }

                // clear existing labels
                oldLabels.length = 0;

                // add all the new labels
                for (let i = 0; i < newLabels.length; i++) {
                    oldLabels.push(newLabels[i]);
                }

                return oldLabels;
            }

            // If it was null/undefined before it can't be done in-place so assignment is required.
            oldChartData.labels = innerFunc(oldChartData.labels!, newChartData.labels!);
            oldChartData.xLabels = innerFunc(oldChartData.xLabels!, newChartData.xLabels!);
            oldChartData.yLabels = innerFunc(oldChartData.yLabels!, newChartData.yLabels!);
        }

        private static wireUpCallbacks(config: BitChartConfiguration) {
            // Replace IMethodHandler objects with actual function (if present)
            // This should be "automated" in some way. We shouldn't have to add
            // (much) new code for a new callback.
            BitChart.wireUpOptionsOnClick(config);
            BitChart.wireUpOptionsOnHover(config);
            BitChart.wireUpLegendOnClick(config);
            BitChart.wireUpLegendOnHover(config);
            BitChart.wireUpLegendItemFilter(config);
            BitChart.wireUpGenerateLabels(config);
            BitChart.wireUpTickCallback(config);
        }

        private static wireUpOptionsOnClick(config: BitChartConfiguration) {
            let getDefaultFunc = (type: any) => {
                let defaults = Chart.defaults[type] || Chart.defaults.global;
                return defaults?.onClick || Chart.defaults.global.onClick;
            };

            if (!config.options) return;

            config.options.onClick = BitChart.getMethodHandler(<IMethodHandler>config.options.onClick, getDefaultFunc(config.type));
        }

        private static wireUpOptionsOnHover(config: BitChartConfiguration) {
            let getDefaultFunc = (type: any) => {
                let defaults = Chart.defaults[type] || Chart.defaults.global;
                return defaults?.onHover || Chart.defaults.global.onHover;
            };

            if (!config.options)
                return;

            config.options.onHover = BitChart.getMethodHandler(<IMethodHandler>config.options.onHover, getDefaultFunc(config.type));
        }

        private static wireUpLegendOnClick(config: BitChartConfiguration) {
            let getDefaultHandler = (type: any) => {
                let chartDefaults = Chart.defaults[type] || Chart.defaults.global;
                return chartDefaults?.legend?.onClick || Chart.defaults.global.legend?.onClick;
            };

            if (!config.options?.legend)
                return;

            config.options.legend.onClick = BitChart.getMethodHandler(<IMethodHandler>config.options.legend.onClick, getDefaultHandler(config.type));
        }

        private static wireUpLegendOnHover(config: BitChartConfiguration) {
            let getDefaultFunc = (type: any) => {
                let chartDefaults = Chart.defaults[type] || Chart.defaults.global;
                return chartDefaults?.legend?.onHover || Chart.defaults.global.legend!.onHover;
            };

            if (!config.options?.legend)
                return;

            config.options.legend.onHover = BitChart.getMethodHandler(<IMethodHandler>config.options.legend.onHover, getDefaultFunc(config.type));
        }

        private static wireUpLegendItemFilter(config: BitChartConfiguration) {
            let getDefaultFunc = (type: any) => {
                let chartDefaults = Chart.defaults[type] || Chart.defaults.global;
                return chartDefaults?.legend?.labels?.filter || Chart.defaults.global.legend!.labels!.filter;
            };

            if (!config.options?.legend?.labels)
                return;

            config.options.legend.labels.filter = BitChart.getMethodHandler(<IMethodHandler>config.options.legend.labels.filter, getDefaultFunc(config.type));
        }

        private static wireUpGenerateLabels(config: BitChartConfiguration) {
            let getDefaultFunc = (type: any) => {
                let chartDefaults = Chart.defaults[type] || Chart.defaults.global;
                return chartDefaults?.legend?.labels?.generateLabels || Chart.defaults.global.legend!.labels!.generateLabels;
            };

            if (!config.options?.legend?.labels)
                return;

            config.options.legend.labels.generateLabels = BitChart.getMethodHandler(<IMethodHandler>config.options.legend.labels.generateLabels, getDefaultFunc(config.type));
        }

        private static wireUpTickCallback(config: BitChartConfiguration) {
            /* Defaults table (found out by checking Chart.defaults in console) -> everything undefined
             * Bar (scales): undefined
             * Bubble (scales): undefined
             * Pie & Doughnut: don't even have scale(s) field
             * HorizontalBar (scales): undefined
             * Line (scales): undefined
             * PolarArea (scale): undefined
             * Radar (scale): undefined
             * Scatter (scales): undefined
             */

            const assignCallbacks = (axes: any) => {
                if (axes) {
                    for (let i = 0; i < axes.length; i++) {
                        if (!axes[i].ticks) continue;
                        axes[i].ticks.callback = BitChart.getMethodHandler(axes[i].ticks.callback, undefined);
                        if (!axes[i].ticks.callback) {
                            delete axes[i].ticks.callback; // undefined != deleted, Chart.js throws an error if it's undefined so we have to delete it
                        }
                    }
                }
            }

            if (config.options?.scales) {
                assignCallbacks(config.options.scales.xAxes);
                assignCallbacks(config.options.scales.yAxes);
            }

            if (config.options?.scale?.ticks) {
                config.options.scale.ticks.callback = BitChart.getMethodHandler(<IMethodHandler>config.options.scale.ticks.callback, undefined);

                if (!config.options.scale.ticks.callback) {
                    delete config.options.scale.ticks.callback; // undefined != deleted, Chart.js throws an error if it's undefined so we have to delete it
                }
            }
        }

        /**
         * Given an IMethodHandler (see C# code), it tries to resolve the referenced method.
         * It currently supports JavaScript functions, which are expected to be attached to the window object, and .Net delegates which can be
         * bound to .Net static functions, .Net object instance methods and more.
         *
         * When failing to recover a method from the IMethodHandler, it returns the default handler.
         *
         * @param handler the serialized IMethodHandler (see C# code)
         * @param defaultFunc the fallback value to use in case the method can't be resolved
         */
        private static getMethodHandler(handler: IMethodHandler, defaultFunc?: Function) {
            if (handler == null) {
                return defaultFunc;
            }

            if (BitChart.isDelegateHandler(handler)) {
                // stringify args and ignore all circular references. This means that objects of type DotNetObject will not be
                // deserialized correctly (since it's already a string when it reaches JSON.stringify in the blazor interop layer)
                // but the values passed to chart callbacks should never contain such objects anyway.
                // Also if we don't care about the value, don't bother to stringify.
                const stringifyArgs = (args: any[]) => {
                    for (let i = 0; i < args.length; i++) {
                        if (handler.ignoredIndices.includes(i)) {
                            args[i] = '';
                        } else {
                            args[i] = BitChart.stringifyObjectIgnoreCircular(args[i]);
                        }
                    }

                    return args;
                }

                if (!handler.returnsValue) {
                    // https://stackoverflow.com/questions/59543973/use-async-function-when-consumer-doesnt-expect-a-promise
                    return (...args: any[]) => handler.handlerReference.invokeMethodAsync(handler.methodName, stringifyArgs(args));
                } else {
                    if (window.hasOwnProperty('MONO')) {
                        return (...args: any[]) => handler.handlerReference.invokeMethod(handler.methodName, stringifyArgs(args)); // only works on client side
                    } else {
                        console.warn('Using C# delegates that return values in chart.js callbacks is not supported on ' +
                            "server side blazor because the server side dispatcher doesn't support synchronous interop calls. Falling back to default value.");

                        return defaultFunc;
                    }
                }
            } else {
                if (handler.methodName == null) {
                    return defaultFunc;
                }

                const namespaceAndFunc: string[] = handler.methodName.split('.');
                if (namespaceAndFunc.length !== 2) {
                    return defaultFunc;
                }

                const namespace = (window as any)[namespaceAndFunc[0]];
                if (namespace == null) {
                    return defaultFunc;
                }

                const func = namespace[namespaceAndFunc[1]];
                if (typeof func === 'function') {
                    return func;
                } else {
                    return defaultFunc;
                }
            }
        }

        private static isDelegateHandler(handler: IMethodHandler): handler is DelegateHandler {
            return 'handlerReference' in handler;
        }

        private static stringifyObjectIgnoreCircular(object: any) {
            const seen = new WeakSet();
            const replacer = (_name: any, value: any) => {
                if (
                    typeof value === 'object' &&
                    value !== null &&
                    !(value instanceof Boolean) &&
                    !(value instanceof Date) &&
                    !(value instanceof Number) &&
                    !(value instanceof RegExp) &&
                    !(value instanceof String)
                ) {
                    if (seen.has(value))
                        return undefined;

                    seen.add(value);
                }

                return value;
            }

            return JSON.stringify(object, replacer);
        }
    }
}
