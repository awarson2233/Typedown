import remote from 'services/remote/common'

class TablePicker {
  static pluginName = 'tablePicker'
  constructor(muya) {
    muya.eventCenter.subscribe('muya-table-picker', async (data, reference, cb) => {
      let size
      try {
        size = await remote.resizeTable({ rows: data.row + 1, columns: data.column + 1 })
      } catch (err) {
        console.error(err)
        return
      }
      // 用户在宿主的尺寸对话框里取消时返回 null：不建表、不改表。
      if (!size) {
        return
      }
      const { rows, columns } = size
      cb(Math.max(rows - 1, 0), Math.max(columns - 1, 0))
    })
  }
}

export default TablePicker
