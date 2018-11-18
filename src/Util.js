export function mkMethod(update, mkProps, mkModel, mkMsg) {
    return function () {
        const oldModel = mkModel(this);
        const newModel = update(this, mkProps(this), oldModel, mkMsg(arguments));
        if (newModel !== oldModel) {
            for (const key of Object.keys(newModel)) {
                if (newModel[key] !== oldModel[key]) {
                    this[key] = newModel[key];
                }
            }
        }
    }
}