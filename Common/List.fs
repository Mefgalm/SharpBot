module Common.List

let remove f = List.filter (not << f)