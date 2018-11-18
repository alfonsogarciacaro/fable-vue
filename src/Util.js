export function mkMethod(update, mkModel, mkMsg) {
    return function () {
        const oldModel = mkModel(this);
        const newModel = update(oldModel, mkMsg(arguments));
        if (newModel !== oldModel) {
            for (const key of Object.keys(newModel)) {
                if (newModel[key] !== oldModel[key]) {
                    this[key] = newModel[key];
                }
            }
        }
    }
}